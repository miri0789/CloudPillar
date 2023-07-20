using common;
using Backend.Iotlistener.Interfaces;
using Shared.Entities.Events;
using Shared.Logger;

namespace Backend.Iotlistener.Services;


public class SigningService : ISigningService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    public SigningService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        ArgumentNullException.ThrowIfNull(httpRequestorService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _environmentsWrapper = environmentsWrapper;
        _httpRequestorService = httpRequestorService;
    }

    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        try
        {
            string requestUrl = $"{_environmentsWrapper.signingUrl}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            _logger.Error($"SigningService CreateTwinKeySignature failed.", ex);
        }
    }

}
