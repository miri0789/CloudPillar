using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Handlers;

public class SymmetricKeyProvisioningHandler : ISymmetricKeyProvisioningHandler
{
    private readonly ILoggerHandler _logger;
    private IDeviceClientWrapper _deviceClientWrapper;
    private ISymmetricKeyWrapper _symmetricKeyWrapper;
    private readonly IProvisioningDeviceClientWrapper _provisioningDeviceClientWrapper;
    private readonly AuthenticationSettings _authenticationSettings;

    public SymmetricKeyProvisioningHandler(ILoggerHandler loggerHandler,
     IDeviceClientWrapper deviceClientWrapper,
     ISymmetricKeyWrapper symmetricKeyWrapper,
     IProvisioningDeviceClientWrapper provisioningDeviceClientWrapper,
     IOptions<AuthenticationSettings> authenticationSettings)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _symmetricKeyWrapper = symmetricKeyWrapper ?? throw new ArgumentNullException(nameof(symmetricKeyWrapper));
        _provisioningDeviceClientWrapper = provisioningDeviceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningDeviceClientWrapper));
        _authenticationSettings = authenticationSettings?.Value ?? throw new ArgumentNullException(nameof(authenticationSettings));
    }

    public async Task<bool> AuthorizationDeviceAsync(CancellationToken cancellationToken)
    {
        return await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
    }

    public async Task ProvisioningAsync(string deviceId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(_authenticationSettings.DpsScopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(_authenticationSettings.GlobalDeviceEndpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(_authenticationSettings.GroupEnrollmentKey);

        var deviceName = deviceId;
        var primaryKey = _authenticationSettings.GroupEnrollmentKey;
        var drivedDevice = ComputeDerivedSymmetricKey(primaryKey, deviceName);



        using (var security = _symmetricKeyWrapper.GetSecurityProvider(deviceName, drivedDevice, null))
        {
            _logger.Debug($"Initializing the device provisioning client...");

            using (ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler())
            {
                DeviceRegistrationResult result = await _provisioningDeviceClientWrapper.RegisterAsync(_authenticationSettings.GlobalDeviceEndpoint,
                    _authenticationSettings.DpsScopeId,
                    security,
                    transport);

                if (result == null)
                {
                    HandleError("RegisterAsync failed");
                }

                _logger.Debug($"Registration status: {result.Status}.");

                if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                {
                    HandleError("Registration status did not assign a hub");
                }

                _logger.Info($"Device {result.DeviceId} registered to {result.AssignedHub}.");
                await InitializeDeviceAsync(result.DeviceId, result.AssignedHub, drivedDevice, cancellationToken);
            }
        }
    }

    private async Task InitializeDeviceAsync(string deviceId, string iotHubHostName, string deviceKey, CancellationToken cancellationToken)
    {
        try
        {
            var auth = _symmetricKeyWrapper.GetDeviceAuthentication(deviceId, deviceKey);
            await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
            await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: ", ex);
        }
    }

    private string ComputeDerivedSymmetricKey(string primaryKey, string registrationId)
    {
        if (string.IsNullOrWhiteSpace(primaryKey))
        {
            return primaryKey;
        }

        var hmac = _symmetricKeyWrapper.CreateHMAC(primaryKey);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(registrationId)));
    }

    private void HandleError(string errorMsg)
    {
        _logger.Error(errorMsg);
        throw new Exception(errorMsg);
    }
}