using common;
using shared.Entities;
using Shared.Logger;

namespace iotlistener;

public interface ISigningService
{
    Task CreateTwinKeySignature(string deviceId, SignEvent signEvent);
}

public class SigningService : ISigningService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly Uri _signingUrl;
    private readonly ILoggerHandler _logger;
    public SigningService(IHttpRequestorService httpRequestorService, ILoggerHandler logger)
    {
        _httpRequestorService = httpRequestorService;
        _signingUrl = new Uri(Environment.GetEnvironmentVariable(Constants.signingUrl)!);

        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        try
        {
            string requestUrl = $"{_signingUrl}signing/create?deviceId={deviceId}&keyPath={signEvent.keyPath}&signatureKey={signEvent.signatureKey}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            _logger.Error($"SigningService CreateTwinKeySignature failed.", ex);
            throw;
        }
    }

}
