using System.Text;
using PriorityQueue.Services.Interfaces;
using PriorityQueue.Wrappers.Interfaces;
using Shared.Logger;

namespace PriorityQueue.Services;
public class MessageProcessor : IMessageProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerHandler _logger;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    public MessageProcessor(HttpClient httpClient,
                            ILoggerHandler logger,
                            IEnvironmentsWrapper environmentsWrapper)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task<bool> ProcessMessageAsync(string message, IDictionary<string, string> properties)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _environmentsWrapper.svcBackendUrl)
        {
            Content = new StringContent(message, Encoding.UTF8, "application/json")
        };

        foreach (var property in properties)
        {
            request.Headers.Add(property.Key, property.Value);
        }
        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var isBadRequest = ((int)response.StatusCode) / 100 == 4;
                if (isBadRequest)
                {
                    _logger.Warn($"ProcessMessageAsync Bad request: {message}");
                }
                else
                {
                    _logger.Error($"ProcessMessageAsync Failed to process message: {message} with status code: {response.StatusCode}");
                }
                return isBadRequest;
            }
            else
            {
                return true; // Processed
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"ProcessMessageAsync Failed to process message: {message} with exception: {ex.Message}");
            return true;
        }

    }
}
