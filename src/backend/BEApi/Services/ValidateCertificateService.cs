using Backend.BEApi.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Twin;
using Shared.Logger;


namespace Backend.BEApi.Services;

public class ValidateCertificateService : IValidateCertificateService
{
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly ILoggerHandler _logger;

    public ValidateCertificateService(IRegistryManagerWrapper registryManagerWrapper, ILoggerHandler logger)
    {
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsCertificateExpiredAsync(string deviceId)
    {
        _logger.Info("Validating certificate...");
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
            var creationDate = twinReported.CertificateValidity.CreationDate;
            var expiredDate = twinReported.CertificateValidity.ExpirationDate;
            var currentDate = DateTime.UtcNow;

            currentDate = currentDate < creationDate ? creationDate : (currentDate > expiredDate ? expiredDate : currentDate);
            TimeSpan totalDuration = expiredDate - creationDate;
            TimeSpan passedDuration = currentDate - creationDate;
            double percentagePassed = (double)passedDuration.Ticks / totalDuration.Ticks;
            return percentagePassed >= 0.6;
        }
    }
}