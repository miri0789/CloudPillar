using Backend.BEApi.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Twin;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class DeviceCertificateService : IDeviceCertificateService
{
    private readonly IRegistrationService _registrationService;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public DeviceCertificateService(IRegistrationService registrationService, IRegistryManagerWrapper registryManagerWrapper, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task IsDevicesCertificateExpiredAsync()
    {
        _logger.Info("Validating all certificates...");
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            var tasks = new List<Task>();
            var devices = await _registryManagerWrapper.GetIotDevicesAsync(registryManager, _environmentsWrapper.maxCountDevices);
            foreach (var device in devices)
            {
                _logger.Info($"Device {device.Id}: Validating certificate...");
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, device.Id);
                var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
                var creationDate = twinReported.CertificateValidity.CreationDate;
                var expiredDate = twinReported.CertificateValidity.ExpirationDate;
                var currentDate = DateTime.UtcNow;

                TimeSpan totalDuration = expiredDate - creationDate;
                TimeSpan passedDuration = currentDate - creationDate;
                double percentagePassed = (double)passedDuration.Ticks / totalDuration.Ticks;
                if (percentagePassed >= _environmentsWrapper.expirationCertificatePercent)
                {
                    _logger.Info($"Device {device.Id}: Certificate is expired. Provisioning new certificate...");
                    tasks.Add(_registrationService.RegisterAsync(device.Id, twinReported.SecretKey));
                }
                else
                {
                    _logger.Info($"Device {device.Id}: Certificate is valid.");
                }
            }
            await Task.WhenAll(tasks);
        }
    }

    public async Task RemoveDeviceAsync(string deviceId)
    {
        _logger.Info($"Deleting device {deviceId}...");
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            await _registryManagerWrapper.RemoveDeviceAsync(registryManager, deviceId);
        }
        _logger.Info($"Device {deviceId} deleted.");
    }
}