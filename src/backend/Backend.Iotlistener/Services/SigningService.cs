using common;
using Backend.Iotlistener.Interfaces;
using shared.Entities.Events;

namespace Backend.Iotlistener.Services;


public class SigningService : ISigningService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    public SigningService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(httpRequestorService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

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
            Console.WriteLine($"SigningService CreateTwinKeySignature failed. Message: {ex.Message}");
        }
    }

}
