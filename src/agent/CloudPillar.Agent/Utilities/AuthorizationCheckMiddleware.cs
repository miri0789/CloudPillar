using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Shared.Logger;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    private ILoggerHandler _logger;

    private IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, ILoggerHandler logger)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        CancellationToken cancellationToken = context?.RequestAborted ?? CancellationToken.None;
        Endpoint endpoint = context.GetEndpoint();
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

        var newUrl = $"https://localhost:{Constants.HTTPS_DEFAULT_PORT}{context.Request.Path}{context.Request.QueryString}";
        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
        context.Response.Headers["Location"] = newUrl;
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
