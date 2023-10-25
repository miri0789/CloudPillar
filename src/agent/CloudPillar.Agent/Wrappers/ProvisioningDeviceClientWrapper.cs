using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class ProvisioningDeviceClientWrapper : IProvisioningDeviceClientWrapper
{

    public async Task<DeviceRegistrationResult> RegisterAsync(string globalDeviceEndpoint, string dpsScopeId, SecurityProvider securityProvider, ProvisioningTransportHandler transport)
    {
        var provClient = ProvisioningDeviceClient.Create(globalDeviceEndpoint, dpsScopeId, securityProvider, transport);

        return await provClient.RegisterAsync();

    }
}