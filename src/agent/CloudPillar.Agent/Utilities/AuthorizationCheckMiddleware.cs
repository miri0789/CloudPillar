using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Azure.Devices.Client.Exceptions;
using Shared.Logger;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _requestDelegate;

    private ILoggerHandler _logger;

    private IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    const string X_DEVICE_ID = "X-device-id";
    const string X_SECRET_KEY = "X-secret-key";
    
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
            var xDeviceId = requestHeaders[X_DEVICE_ID];
            var xSecretKey = requestHeaders[X_SECRET_KEY];

            if (!string.IsNullOrEmpty(xDeviceId) || !string.IsNullOrEmpty(xSecretKey))
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

            await _requestDelegate(context);
        }
        else
        {
            await _requestDelegate(context);
        }

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
