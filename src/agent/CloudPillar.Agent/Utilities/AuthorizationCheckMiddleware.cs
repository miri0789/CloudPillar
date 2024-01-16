using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text.RegularExpressions;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Sevices.Interfaces;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;
    private ILoggerHandler _logger;
    private readonly IConfiguration _configuration;
    private ISymmetricKeyProvisioningHandler? _symmetricKeyProvisioningHandler;
    private IProvisioningService? _provisioningService;

    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, ILoggerHandler logger, IConfiguration configuration)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    }

    public async Task Invoke(HttpContext context, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, IStateMachineHandler stateMachineHandler, IX509Provider x509Provider)
    {
        _provisioningService = context.RequestServices.GetRequiredService<IProvisioningService>();
        ArgumentNullException.ThrowIfNull(_provisioningService);
        _symmetricKeyProvisioningHandler = context.RequestServices.GetRequiredService<ISymmetricKeyProvisioningHandler>();
        ArgumentNullException.ThrowIfNull(_symmetricKeyProvisioningHandler);

        if (!context.Request.IsHttps)
        {
            NextWithRedirectAsync(context, x509Provider);
            return;
        }
        var endpoint = context.GetEndpoint();
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
        ArgumentNullException.ThrowIfNull(dPSProvisioningDeviceClientHandler);
        CancellationToken cancellationToken = context.RequestAborted;
        if (IsActionMethod(endpoint))
        {
            // check the headers for all the actions also for the AllowAnonymous.
            IHeaderDictionary requestHeaders = context.Request.Headers;
            var xDeviceId = requestHeaders.TryGetValue(Constants.X_DEVICE_ID, out var deviceId) ? deviceId.ToString() : string.Empty;
            var xSecretKey = requestHeaders.TryGetValue(Constants.X_SECRET_KEY, out var secretKey) ? secretKey.ToString() : string.Empty;

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
            var action = context.Request.Path.Value?.ToLower() ?? "";
            var actionName = context.Request.Path.Value?.Split("/").LastOrDefault();
            var checkAuthorization = deviceIsBusy && (action.Contains("setready") == true || action.Contains("setbusy") == true);
            var x509Certificate = dPSProvisioningDeviceClientHandler.GetCertificate();
            bool isX509Authorized = await dPSProvisioningDeviceClientHandler.AuthorizationDeviceAsync(xDeviceId, xSecretKey, cancellationToken, checkAuthorization);
            if (!isX509Authorized)
            {
                if (x509Certificate is not null)
                {
                    await UnauthorizedResponseAsync(context, $"{actionName}, The deviceId or the SecretKey are incorrect.");
                    return;
                }

                _logger.Info($"{actionName}, The device is X509 unAuthorized, check symmetric key authorized");
                var isSymetricKeyAuthorized = await _symmetricKeyProvisioningHandler.AuthorizationDeviceAsync(cancellationToken);
                if (!isSymetricKeyAuthorized)
                {
                    _logger.Info($"{actionName}, The device is symmetric key unAuthorized, start provisinig proccess");
                    await _provisioningService.ProvisinigSymetricKeyAsync(cancellationToken);
                }
                if (!action.Contains("getdevicestate"))
                {
                    await UnauthorizedResponseAsync(context, $"{actionName}, Symmetric key is Unauthorized.");
                    return;
                }
            }
            if (x509Certificate?.NotAfter <= DateTime.UtcNow)
            {
                await _provisioningService.ProvisinigSymetricKeyAsync(cancellationToken);
                if (!action.Contains("getdevicestate"))
                {
                    await UnauthorizedResponseAsync(context, $"{actionName}, The certificate is expired.");
                    return;
                }
            }
            await _requestDelegate(context);
        }
        else
        {
            await _requestDelegate(context);
        }
    }
    private void NextWithRedirectAsync(HttpContext context, IX509Provider x509Provider)
    {
        context.Connection.ClientCertificate = x509Provider.GetHttpsCertificate();

        var sslPort = _configuration.GetValue(Constants.HTTPS_CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
        var uriBuilder = new UriBuilder(context.Request.GetDisplayUrl())
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
