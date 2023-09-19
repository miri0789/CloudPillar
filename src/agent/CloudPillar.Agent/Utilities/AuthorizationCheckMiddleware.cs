using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Shared.Logger;

namespace CloudPillar.Agent.Utilities;
public class AuthorizationCheckMiddleware
{
    private readonly RequestDelegate _next;

    private ILoggerHandler _logger;

    private IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;

    public AuthorizationCheckMiddleware(RequestDelegate next,  IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler, ILoggerHandler logger)
    {
        _next = next;
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(HttpContext context)
    {
        
            Endpoint endpoint = context.GetEndpoint();
            if (endpoint != null && endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                // The action has [AllowAnonymous], so allow the request to proceed
                await _next(context);
                return;
            }

            X509Certificate2 userCertificate = _dPSProvisioningDeviceClientHandler.GetCertificate();

            if (userCertificate == null)
            {
                var error = "no certificate found in the store";
                _logger.Error(error);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(error);
                return;
            }

            bool isAuthorized = await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(userCertificate);
            if (!isAuthorized)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var error = "User is not authorized.";
                _logger.Error(error);
                await context.Response.WriteAsync(error);
                return;
            }

            await _next(context);       
        
    }

}
