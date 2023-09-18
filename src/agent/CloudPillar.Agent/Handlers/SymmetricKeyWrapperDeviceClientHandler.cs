
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
namespace CloudPillar.Agent.Handlers;

public class SymmetricKeyWrapperDeviceClientHandler : ISymmetricKeyWrapperDeviceClientHandler
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;

    public SymmetricKeyWrapperDeviceClientHandler(ILoggerHandler loggerHandler, IDeviceClientWrapper deviceClientWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
    }

    public Task<bool> AuthorizationAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public async Task ProvisionWithSymmetricKeyAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint)
    {
        using (var security = new SecurityProviderSymmetricKey(registrationId, primaryKey, null))
        {
            // using (var transport = new ProvisioningTransportHandlerMqtt())
            using ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler();
            {
                var provisioningClient = ProvisioningDeviceClient.Create(globalDeviceEndpoint, scopeId, security, transport);

                var result = await provisioningClient.RegisterAsync();

                if (result.Status == ProvisioningRegistrationStatusType.Assigned)
                {
                    var connectionString = $"HostName={result.AssignedHub};DeviceId={result.DeviceId};SharedAccessKey={primaryKey}";
                    // Save the connection string or use it to connect to IoT Hub.
                }
                else
                {
                    // Provisioning failed. Handle the error.
                }
            }
        }
    }
}