using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class SymmetricKeyWrapper : ISymmetricKeyWrapper
{
    public SecurityProviderSymmetricKey GetSecurityProvider(string registrationId, string primaryKey, string? secondKey)
    {
        throw new NotImplementedException();
    }
}
