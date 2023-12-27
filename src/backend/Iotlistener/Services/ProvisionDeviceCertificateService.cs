using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Interfaces;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.Iotlistener.Services;


public class ProvisionDeviceCertificateService : IProvisionDeviceCertificateService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    public ProvisionDeviceCertificateService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task ProvisionDeviceCertificateAsync(string deviceId, ProvisionDeviceCertificateEvent provisionEvent)
    {
        ArgumentNullException.ThrowIfNull(provisionEvent);
        ArgumentNullException.ThrowIfNull(provisionEvent.Data);
        try
        {
            string requestUrl = $"{_environmentsWrapper.beApiUrl}RegisterByCertificate/ProvisionDeviceCertificate?deviceId={deviceId}&prefix={provisionEvent.CertificatePrefix}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, provisionEvent.Data);
        }
        catch (Exception ex)
        {
            _logger.Error($"ProvisionDeviceCertificateService failed.", ex);
        }
    }

}
