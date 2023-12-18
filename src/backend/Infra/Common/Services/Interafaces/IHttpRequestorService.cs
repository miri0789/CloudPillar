namespace Backend.Infra.Common.Services.Interfaces;

public interface IHttpRequestorService
{

    Task SendRequest(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default);
    Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default);
}