using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Shared.Logger;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    private ILoggerHandler _logger;
    private readonly IConfiguration _configuration;
    private IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, ILoggerHandler logger, IConfiguration configuration)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task Invoke(HttpContext context)
    {
        CancellationToken cancellationToken = context?.RequestAborted ?? CancellationToken.None;
        Endpoint endpoint = context.GetEndpoint();

        IHeaderDictionary requestHeaders = context.Request.Headers;
        var xDeviceId = string.Empty;
        var xSecretKey = string.Empty;
        if (requestHeaders.ContainsKey(AuthorizationConstants.X_DEVICE_ID))
        {
            xDeviceId = requestHeaders[AuthorizationConstants.X_DEVICE_ID];
        }
        if (requestHeaders.ContainsKey(AuthorizationConstants.X_SECRET_KEY))
        {
            xSecretKey = requestHeaders[AuthorizationConstants.X_SECRET_KEY];
        }
        if (string.IsNullOrEmpty(xDeviceId) || string.IsNullOrEmpty(xSecretKey))
        {
            var error = "No require header was provided";
            await UnauthorizedResponseAsync(context, error);
            return;
        }
        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            // The action has [AllowAnonymous], so allow the request to proceed
            await _requestDelegate(context);
            return;
        }
        if (IsActionMethod(endpoint))
        {

            X509Certificate2? userCertificate = _dPSProvisioningDeviceClientHandler.GetCertificate();

            if (userCertificate == null)
            {
                var error = "no certificate found in the store";
                await UnauthorizedResponseAsync(context, error);
                return;
            }
            IHeaderDictionary requestHeaders = context.Request.Headers;
            var xDeviceId = string.Empty;
            var xSecretKey = string.Empty;
            if (requestHeaders.ContainsKey(Constants.X_DEVICE_ID))
            {
                xDeviceId = requestHeaders[Constants.X_DEVICE_ID];
            }
            if (requestHeaders.ContainsKey(Constants.X_SECRET_KEY))
            {
                xSecretKey = requestHeaders[Constants.X_SECRET_KEY];
            }
            if (string.IsNullOrEmpty(xDeviceId) || string.IsNullOrEmpty(xSecretKey))
            {
                var error = "No require header was provided";
                await UnauthorizedResponseAsync(context, error);
                return;
            }

            bool isAuthorized = await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(userCertificate, xDeviceId, xSecretKey, cancellationToken);
            if (!isAuthorized)
            {
                var error = "User is not authorized.";
                await UnauthorizedResponseAsync(context, error);
                return;
            }

            await NextWithRedirectAsync(context);
        }
        else
        {
            await NextWithRedirectAsync(context);
        }
    }
    private async Task NextWithRedirectAsync(HttpContext context)
    {
        if (context.Request.IsHttps)
        {
            await _requestDelegate(context);
            return;
        }

        var port = _configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTP_DEFAULT_PORT);
        var sslPort = _configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
        var newUrl = context.Request.GetDisplayUrl().Replace("http", "https").Replace(port.ToString(), sslPort.ToString());
        context.Response.Redirect(newUrl, false, true);
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
}
