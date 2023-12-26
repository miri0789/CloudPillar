using Backend.BEApi.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Twin;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class ValidateCertificateService : IValidateCertificateService
{
    private readonly IRegistrationService _registrationService;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public ValidateCertificateService(IRegistrationService registrationService, IRegistryManagerWrapper registryManagerWrapper, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
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
            var devices = await _registryManagerWrapper.GetIotDevicesAsync(registryManager);
            foreach (var device in devices)
            {
                await IsCertificateExpiredAsync(device.Id);
            }
        }
    }

    private async Task IsCertificateExpiredAsync(string deviceId)
    {
        _logger.Info($"Device {deviceId}: Validating certificate...");
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
            var creationDate = twinReported.CertificateValidity.CreationDate;
            var expiredDate = twinReported.CertificateValidity.ExpirationDate;
            var currentDate = DateTime.UtcNow;

            TimeSpan totalDuration = expiredDate - creationDate;
            TimeSpan passedDuration = currentDate - creationDate;
            double percentagePassed = (double)passedDuration.Ticks / totalDuration.Ticks;
            var isExpired = percentagePassed >= _environmentsWrapper.expirationCertificatePercent;
            if (isExpired)
            {
                _logger.Info("Certificate is expired. Provisioning new certificate...");
                await _registrationService.RegisterAsync(deviceId, twinReported.SecretKey);
            }
            else
            {
                _logger.Info("Certificate is valid.");
            }
        }
    }
}