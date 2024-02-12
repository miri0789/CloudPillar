using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text.RegularExpressions;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Sevices.Interfaces;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private ILoggerHandler _logger;
    private readonly IConfigurationWrapper _configuration;
    private ISymmetricKeyProvisioningHandler? _symmetricKeyProvisioningHandler;
    private IProvisioningService? _provisioningService;
    private IHttpContextWrapper? _httpContextWrapper;
    private IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, ILoggerHandler logger, IConfigurationWrapper configuration)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    }

    public async Task Invoke(HttpContext context, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, IStateMachineHandler stateMachineHandler,
    IX509Provider x509Provider, IHttpContextWrapper httpContextWrapper)
    {
        _provisioningService = context.RequestServices.GetRequiredService<IProvisioningService>();
        ArgumentNullException.ThrowIfNull(_provisioningService);
        _symmetricKeyProvisioningHandler = context.RequestServices.GetRequiredService<ISymmetricKeyProvisioningHandler>();
        ArgumentNullException.ThrowIfNull(_symmetricKeyProvisioningHandler);
        _httpContextWrapper = httpContextWrapper ?? throw new ArgumentNullException(nameof(httpContextWrapper));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));

        if (!context.Request.IsHttps)
        {
            NextWithRedirectAsync(context, x509Provider);
            return;
        }
        var endpoint = _httpContextWrapper.GetEndpoint(context);
        //context
        var deviceIsBusy = stateMachineHandler.GetCurrentDeviceState() == DeviceStateType.Busy;
        if (deviceIsBusy)
        {
            var isActionBlockByBusy = endpoint?.Metadata.GetMetadata<DeviceStateFilterAttribute>();
            if (isActionBlockByBusy != null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync(StateMachineConstants.BUSY_MESSAGE);
                return;
            }
        }
        CancellationToken cancellationToken = context.RequestAborted;
        if (IsActionMethod(endpoint))
        {
            // check the headers for all the actions also for the AllowAnonymous.
            IHeaderDictionary requestHeaders = context.Request.Headers;
            var xDeviceId = _httpContextWrapper.TryGetValue(requestHeaders, Constants.X_DEVICE_ID, out var deviceId) ? deviceId.ToString() : string.Empty;
            var xSecretKey = _httpContextWrapper.TryGetValue(requestHeaders, Constants.X_SECRET_KEY, out var secretKey) ? secretKey.ToString() : string.Empty;

            if (string.IsNullOrEmpty(xDeviceId) || string.IsNullOrEmpty(xSecretKey))
            {
                var error = "Require headers were not provided";
                await UnauthorizedResponseAsync(context, error);
                return;
            }

            if (!await IsValidDeviceId(context, xDeviceId))
            {
                return;
            }

            if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                // The action has [AllowAnonymous], so allow the request to proceed
                await _requestDelegate(context);
                return;
            }
            var actionName = context.Request.Path.Value?.Split("/").LastOrDefault();
            var isAuthorized = await IsAuthorized(context, deviceIsBusy, xDeviceId, xSecretKey, actionName, cancellationToken);
            if (!isAuthorized)
            {
                await UnauthorizedResponseAsync(context, $"{actionName}, Unauthorized device.");
                return;
            }
            await _requestDelegate(context);
        }
        else
        {
            await _requestDelegate(context);
        }
    }

    private async Task<bool> IsAuthorized(HttpContext context, bool deviceIsBusy, string xDeviceId, string xSecretKey, string actionName, CancellationToken cancellationToken)
    {
        var action = context.Request.Path.Value?.ToLower() ?? "";
        var checkAuthorization = deviceIsBusy && (action.Contains("setready") == true || action.Contains("setbusy") == true);
        var x509Certificate = _dPSProvisioningDeviceClientHandler.GetCertificate();
        DeviceConnectResultEnum connectRes = await _dPSProvisioningDeviceClientHandler.AuthorizationDeviceAsync(xDeviceId, xSecretKey, cancellationToken, checkAuthorization);
        if (connectRes != DeviceConnectResultEnum.Valid)
        {
            if (x509Certificate is not null)
            {
                if (connectRes == DeviceConnectResultEnum.DeviceNotFound)
                {
                    _logger.Info($"Device {xDeviceId} is not found, start provisioning");
                    return await StartProvisioning(action, cancellationToken);
                }
                return false;
            }

            _logger.Info($"{actionName}, The device is X509 unAuthorized, check symmetric key authorized");
            connectRes = await _symmetricKeyProvisioningHandler.AuthorizationDeviceAsync(cancellationToken);
            if (connectRes != DeviceConnectResultEnum.Valid)
            {
                _logger.Info($"{actionName}, The device is symmetric key unAuthorized, start provisinig proccess");
                return await StartProvisioning(action, cancellationToken);
            }
            return action.Contains("getdevicestate");
        }
        if (x509Certificate?.NotAfter <= DateTime.UtcNow)
        {
            _logger.Info($"{actionName}, The certificate is expired, start provisinig proccess");
            return await StartProvisioning(action, cancellationToken);
        }
        return true;
    }


    private async Task<bool> StartProvisioning(string action, CancellationToken cancellationToken)
    {
        await _provisioningService.ProvisinigSymetricKeyAsync(cancellationToken);
        return action.Contains("getdevicestate");
    }
    private void NextWithRedirectAsync(HttpContext context, IX509Provider x509Provider)
    {
        context.Connection.ClientCertificate = x509Provider.GetHttpsCertificate();

        var sslPort = _configuration.GetValue(Constants.HTTPS_CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
        var uriBuilder = new UriBuilder(_httpContextWrapper.GetDisplayUrl(context))
        {
            Scheme = Uri.UriSchemeHttps,
            Port = sslPort,
        };

        context.Response.Redirect(uriBuilder.Uri.AbsoluteUri, false, true);
    }

    private async Task UnauthorizedResponseAsync(HttpContext context, string error)
    {
        _logger.Error(error);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(error);
    }

    private bool IsActionMethod(Endpoint? endpoint)
    {
        if (endpoint == null)
        {
            return false;
        }

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        return actionDescriptor != null;
    }

    private async Task<bool> IsValidDeviceId(HttpContext context, string deviceId)
    {
        string pattern = @"^[A-Za-z0-9\-:.+%_#*?!(),=$']{1,128}$";
        var regex = new Regex(pattern);
        if (!regex.IsMatch(deviceId))
        {
            var error = "Device ID contains one or more invalid character. ID may contain [Aa-Zz] [0-9] and [-:.+%_#*?!(),=$']";
            _logger.Error(error);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(error);
            return false;
        }
        return true;

    }

}
