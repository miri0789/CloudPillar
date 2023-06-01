using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace common;


//TODO: enforcement of some kind of schema for the calls with Newtonsoft
public interface IHttpRequestorService
{
    Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null);
}

public class HttpRequestorService : IHttpRequestorService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpRequestorService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null)
    {
        HttpClient client = _httpClientFactory.CreateClient();

        HttpRequestMessage request = new HttpRequestMessage(method, url);

        if (requestData != null)
        {
            string serializedData = JsonConvert.SerializeObject(requestData);
            request.Content = new StringContent(serializedData, System.Text.Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            TResponse result = JsonConvert.DeserializeObject<TResponse>(responseContent);
            return result;
        }

        throw new Exception($"HTTP request failed with status code {response.StatusCode}");
    }
}
