using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class SymmetricKeyProvisioningHandler  : ISymmetricKeyProvisioningHandler 
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;
    private ISymmetricKeyWrapper _symmetricKeyWrapper;


    public SymmetricKeyProvisioningHandler (ILoggerHandler loggerHandler,
     IDeviceClientWrapper deviceClientWrapper,
     ISymmetricKeyWrapper symmetricKeyWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _symmetricKeyWrapper = symmetricKeyWrapper ?? throw new ArgumentNullException(nameof(symmetricKeyWrapper));
    }

    public async Task<bool> AuthorizationAsync(CancellationToken cancellationToken)
    {
        bool res = await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        return res;
    }

    public async Task ProvisionWithSymmetricKeyAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(registrationId);
        ArgumentNullException.ThrowIfNullOrEmpty(primaryKey);
        ArgumentNullException.ThrowIfNullOrEmpty(scopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(globalDeviceEndpoint);

        var security = _symmetricKeyWrapper.GetSecurityProvider(registrationId, primaryKey, null);

        _logger.Debug($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler();
        {
            var provisioningClient = ProvisioningDeviceClient.Create(globalDeviceEndpoint, scopeId, security, transport);

            _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

            var result = await provisioningClient.RegisterAsync();

            _logger.Debug($"Registration status: {result.Status}.");

            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                _logger.Error("Registration status did not assign a hub.");
                return;
            }
            await CheckAuthorizationAndInitializeDeviceAsync(result.DeviceId, result.AssignedHub, primaryKey, cancellationToken);
        }
    }

    private async Task<bool> CheckAuthorizationAndInitializeDeviceAsync(string deviceId, string iotHubHostName, string deviceKey, CancellationToken cancellationToken)
    {
        try
        {
            var auth = _symmetricKeyWrapper.GetDeviceAuthentication(deviceId, deviceKey);
            await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
            return await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: ", ex);
            return false;
        }
    }
}