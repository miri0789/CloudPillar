using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text.RegularExpressions;
using Shared.Logger;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    private ILoggerHandler _logger;
    private readonly IConfiguration _configuration;

    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, ILoggerHandler logger, IConfiguration configuration)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    }

    public async Task Invoke(HttpContext context, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, IStateMachineHandler stateMachineHandler, IX509Provider x509Provider)
    {
        Endpoint endpoint = context.GetEndpoint();
        //context
        var deviceIsBusy = stateMachineHandler.GetCurrentDeviceState() == DeviceStateType.Busy;
        if (deviceIsBusy)
        {
            DeviceStateFilterAttribute isActionBlockByBusy = endpoint.Metadata.GetMetadata<DeviceStateFilterAttribute>();
            if (isActionBlockByBusy != null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync(StateMachineConstants.BUSY_MESSAGE);
                return;
            }
        }
        ArgumentNullException.ThrowIfNull(dPSProvisioningDeviceClientHandler);
        CancellationToken cancellationToken = context?.RequestAborted ?? CancellationToken.None;
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


            bool isAuthorized = await dPSProvisioningDeviceClientHandler.AuthorizationDeviceAsync(xDeviceId, xSecretKey, cancellationToken);
            if (!isAuthorized)
            {
                var error = "User is not authorized.";
                await UnauthorizedResponseAsync(context, error);
                return;
            }

            await NextWithRedirectAsync(context, dPSProvisioningDeviceClientHandler, x509Provider);
        }
        else
        {
            await NextWithRedirectAsync(context, dPSProvisioningDeviceClientHandler, x509Provider);
        }
    }
    private async Task NextWithRedirectAsync(HttpContext context, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, IX509Provider x509Provider)
    {
        context.Connection.ClientCertificate = x509Provider.GetHttpsCertificate();
        if (context.Request.IsHttps)
        {
            await _requestDelegate(context);
            return;
        }

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
        string pattern = @"^[A-Za-z0-9\-:.+%_#*?!(),=@$']{1,128}$";
        var regex = new Regex(pattern);
        if (!regex.IsMatch(deviceId))
        {
            var error = "Device ID contains one or more invalid character. ID may contain [Aa-Zz] [0-9] and [-:.+%_#*?!(),=@$']";
            _logger.Error(error);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(error);
            return false;
        }
        return true;

    }

}
