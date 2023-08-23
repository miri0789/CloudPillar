using Newtonsoft.Json;
using System.Text;
using Shared.Logger;

namespace common;

public interface IHttpRequestorService
{

    Task SendRequest(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default);
    Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default);
}

public class HttpRequestorService : IHttpRequestorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISchemaValidator _schemaValidator;
    private readonly ILoggerHandler _logger;

    public HttpRequestorService(IHttpClientFactory httpClientFactory, ISchemaValidator schemaValidator, ILoggerHandler logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(schemaValidator);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _schemaValidator = schemaValidator;
        _logger = logger;
    }

    public async Task SendRequest(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default)
    {
        await SendRequest<object>(url, method, requestData, cancellationToken);
    }

    public async Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient();

        HttpRequestMessage request = new HttpRequestMessage(method, url);
        string schemaPath = "";
        try
        {
            schemaPath = $"{request.RequestUri.Host}/{method.Method}{request.RequestUri.AbsolutePath.Replace("/", "_")}";
        }
        catch (System.InvalidOperationException ex)
        {
            throw new InvalidOperationException("Invalid Url", ex);
        }

        if (requestData != null)
        {
            string serializedData = JsonConvert.SerializeObject(requestData);
            var isRequestValid = _schemaValidator.ValidatePayloadSchema(serializedData, schemaPath, true);
            if (!isRequestValid)
            {
                throw new HttpRequestException("The request data is not fit the schema", null, System.Net.HttpStatusCode.BadRequest);
            }
            request.Content = new StringContent(serializedData, Encoding.UTF8, "application/json");
        }

        if (request?.RequestUri?.Scheme == "https")
        {
            string httpsTimeoutSecondsString = Environment.GetEnvironmentVariable(CommonConstants.httpsTimeoutSeconds);
            int httpsTimeoutSeconds = int.TryParse(httpsTimeoutSecondsString, out int parsedValue) ? parsedValue : 30;
            client.Timeout = TimeSpan.FromSeconds(httpsTimeoutSeconds);
        }

        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (ArgumentNullException ex)
        {
            throw new ArgumentNullException("The request is null", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new ArgumentNullException("The request message was already sent by the HttpClient instance", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ArgumentNullException("The request failed due to an underlying issue", ex);
        }
        catch (Exception ex)
        {
            throw ex;
        }      
        if (response!= null && response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            var isResponseValid = _schemaValidator.ValidatePayloadSchema(responseContent, schemaPath, false);
            if (!isResponseValid)
            {
                throw new HttpRequestException("The reponse data is not fit the schema", null, System.Net.HttpStatusCode.Unauthorized);
            }
            TResponse result = JsonConvert.DeserializeObject<TResponse>(responseContent);
            return result;
        }
        throw new Exception($"HTTP request failed with status code {response?.StatusCode}");
    }
}
