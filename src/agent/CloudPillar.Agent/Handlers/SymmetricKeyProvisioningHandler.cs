using System.Security.Cryptography;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class SymmetricKeyProvisioningHandler : ISymmetricKeyProvisioningHandler
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;
    private ISymmetricKeyWrapper _symmetricKeyWrapper;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    public SymmetricKeyProvisioningHandler(ILoggerHandler loggerHandler,
     IDeviceClientWrapper deviceClientWrapper,
     ISymmetricKeyWrapper symmetricKeyWrapper,
     IEnvironmentsWrapper environmentsWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _symmetricKeyWrapper = symmetricKeyWrapper ?? throw new ArgumentNullException(nameof(symmetricKeyWrapper));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper)); ;
    }

    public async Task<bool> AuthorizationAsync(CancellationToken cancellationToken)
    {
        bool res = await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        return res;
    }

    public async Task ProvisioningAsync(string deviceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.dpsScopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.globalDeviceEndpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.groupEnrollmentPrimaryKey);

        var deviceName = deviceId;
        var primaryKey = _environmentsWrapper.groupEnrollmentPrimaryKey;
        var drivedDevice = ComputeDerivedSymmetricKey(primaryKey, deviceName);



        using (var security = _symmetricKeyWrapper.GetSecurityProvider(deviceName, drivedDevice, null))
        {
            _logger.Debug($"Initializing the device provisioning client...");

            using (ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler())
            {
                var provisioningClient = ProvisioningDeviceClient.Create(_environmentsWrapper.globalDeviceEndpoint, _environmentsWrapper.dpsScopeId, security, transport);

                _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

                var result = await provisioningClient.RegisterAsync();

                _logger.Debug($"Registration status: {result.Status}.");

                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    _logger.Error("Registration status did not assign a hub.");
                    return;
                }
                await CheckAuthorizationAndInitializeDeviceAsync(result.DeviceId, result.AssignedHub, drivedDevice, cancellationToken);
            }
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

    private static string ComputeDerivedSymmetricKey(string primaryKey, string registrationId)
    {
        if (string.IsNullOrWhiteSpace(primaryKey))
        {
            return primaryKey;
        }

        using var hmac = new HMACSHA256(Convert.FromBase64String(primaryKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
    }
}