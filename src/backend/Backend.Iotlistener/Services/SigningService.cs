using common;
using Backend.Iotlistener.Interfaces;
using Shared.Entities.Messages;
// using Shared.Logger;

namespace Backend.Iotlistener.Services;


public class SigningService : ISigningService
{

    // private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    // private readonly ILoggerHandler _logger;
    public SigningService(IEnvironmentsWrapper environmentsWrapper)
    { 
        // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        // _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        try
        {
            string requestUrl = $"{_environmentsWrapper.signingUrl}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";
            // await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            // _logger.Error($"SigningService CreateTwinKeySignature failed.", ex);
        }
    }

}
