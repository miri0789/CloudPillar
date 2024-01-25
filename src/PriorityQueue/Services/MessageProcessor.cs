using System.Text;
using PriorityQueue.Services.Interfaces;
using PriorityQueue.Wrappers.Interfaces;
using Shared.Logger;
using PriorityQueue.Entities.Enums;

namespace PriorityQueue.Services;
public class MessageProcessor : IMessageProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerHandler _logger;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    private const string RELATIVE_URI_PROP = "RelativeURI";
    public MessageProcessor(HttpClient httpClient,
                            ILoggerHandler logger,
                            IEnvironmentsWrapper environmentsWrapper)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task<(MessageProcessType type, string? response, IDictionary<string, string>? responseHeaers)>
    ProcessMessageAsync(string message, IDictionary<string, string> properties, CancellationToken stoppingToken)
    {
        var url = _environmentsWrapper.svcBackendUrl + (properties.TryGetValue(RELATIVE_URI_PROP, out var relativeUri) ? relativeUri : "");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(message, Encoding.UTF8, "application/json")
        };

        foreach (var property in properties.Where(p => p.Key != RELATIVE_URI_PROP))
        {
            request.Headers.Add(property.Key, property.Value);
        }

        try
        {
            _logger.Info($"ProcessMessageAsync Sending message: {message} to url: {url}");
            var response = await _httpClient.SendAsync(request, stoppingToken);
            var timeout = _environmentsWrapper.requestTimeoutMS;

            string responseContent = await response.Content.ReadAsStringAsync();
            var responseHeaers = response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault());
            var returnProcessType = MessageProcessType.ConsumeSuccess;
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warn($"ProcessMessageAsync not success status code: {response.StatusCode} with content: {responseContent} and headers: {responseHeaers}");
                var isBadRequest = ((int)response.StatusCode) / 100 == 4;
                returnProcessType = isBadRequest ? MessageProcessType.ConsumeErrorFatal : MessageProcessType.ConsumeErrorRecoverable;
            }
            return (returnProcessType, responseContent, responseHeaers);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.Error($"ProcessMessageAsync Connection error: {httpEx.Message}");
            return (MessageProcessType.Retain, null, null);
        }
        catch (Exception ex)
        {
            _logger.Error($"ProcessMessageAsync Failed to process message: {message} with exception: {ex.Message}");
            return (MessageProcessType.ConsumeErrorRecoverable, null, null);
        }

    }
}
