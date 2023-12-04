using Newtonsoft.Json;
using System.Text;
using Shared.Logger;
using Backend.Infra.Common.Services.Interfaces;


namespace Backend.Infra.Common.Services;

public class HttpRequestorService : IHttpRequestorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISchemaValidator _schemaValidator;
    private readonly ILoggerHandler _logger;

    public HttpRequestorService(IHttpClientFactory httpClientFactory, ISchemaValidator schemaValidator, ILoggerHandler logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendRequest(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default)
    {
        await SendRequest<object>(url, method, requestData, cancellationToken);
    }

    public async Task<TResponse> SendRequest<TResponse>(string url, HttpMethod method, object? requestData = null, CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromHours(1);

        HttpRequestMessage request = new HttpRequestMessage(method, url);
        string schemaPath = $"{request.RequestUri.Host}/{method.Method}{request.RequestUri.AbsolutePath.Replace("/", "_")}";

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

        HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                var isResponseValid = _schemaValidator.ValidatePayloadSchema(responseContent, schemaPath, false);
                if (!isResponseValid)
                {
                    _logger.Error($"The reponse data is not fit the schema. url: {url}");
                    throw new HttpRequestException("The reponse data is not fit the schema", null, System.Net.HttpStatusCode.Unauthorized);
                }
            }
            TResponse result = JsonConvert.DeserializeObject<TResponse>(responseContent);
            return result;
        }

        _logger.Error($"HTTP request failed: {response.ReasonPhrase}: {method}{url} {responseContent}");
        throw new HttpRequestException($"HTTP request failed: {response.ReasonPhrase}", null, response.StatusCode);
    }
}
