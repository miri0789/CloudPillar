using common;
using iotlistener.Interfaces;
using shared.Entities.Events;

namespace iotlistener.Services;


public class SigningService : ISigningService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly Uri _signingUrl;
    public SigningService(IHttpRequestorService httpRequestorService)
    {
        ArgumentNullException.ThrowIfNull(httpRequestorService);
        _httpRequestorService = httpRequestorService;
        _signingUrl = new Uri(Environment.GetEnvironmentVariable(Constants.signingUrl)!);
    }

    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        try
        {
            string requestUrl = $"{_signingUrl}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SigningService CreateTwinKeySignature failed. Message: {ex.Message}");
        }
    }

}
