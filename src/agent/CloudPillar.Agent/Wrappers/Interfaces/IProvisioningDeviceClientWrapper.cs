using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IProvisioningDeviceClientWrapper
{
        Task<DeviceRegistrationResult> RegisterAsync(string globalDeviceEndpoint, string dpsScopeId, SecurityProvider securityProvider, ProvisioningTransportHandler transport);
}