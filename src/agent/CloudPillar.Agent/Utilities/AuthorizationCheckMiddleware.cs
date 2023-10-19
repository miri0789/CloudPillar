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


    public AuthorizationCheckMiddleware(RequestDelegate requestDelegate, ILoggerHandler logger)
    {
        _requestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context,IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler)
    {
        ArgumentNullException.ThrowIfNull(dPSProvisioningDeviceClientHandler);
        CancellationToken cancellationToken = context?.RequestAborted ?? CancellationToken.None;
        Endpoint endpoint = context.GetEndpoint();
        if (IsActionMethod(endpoint))
        {
            // check the headers for all the actions also for the AllowAnonymous.
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


            bool isAuthorized = await dPSProvisioningDeviceClientHandler.AuthorizationAsync(xDeviceId, xSecretKey, cancellationToken);
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
