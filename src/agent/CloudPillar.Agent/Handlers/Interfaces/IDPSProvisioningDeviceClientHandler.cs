
using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface IDPSProvisioningDeviceClientHandler
{
    Task ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate);

    X509Certificate2 Authenticate();

    bool Authorization(X509Certificate2 userCertificate);
}