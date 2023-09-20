
using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface IDPSProvisioningDeviceClientHandler
{
    Task<bool> ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint);

    X509Certificate2 GetCertificate();

    Task<bool> AuthorizationAsync(X509Certificate2 userCertificate);
}