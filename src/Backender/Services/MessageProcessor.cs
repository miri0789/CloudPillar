using System.Text;
using Backender.Services.Interfaces;
using Backender.Wrappers.Interfaces;
using Shared.Logger;
using Backender.Entities.Enums;

namespace Backender.Services;
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
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken,
                new CancellationTokenSource(new TimeSpan(0, 0, _environmentsWrapper.requestTimeoutSeconds)).Token))
            {
                var response = await _httpClient.SendAsync(request, stoppingToken);
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
        }
        catch (TaskCanceledException ex)
        {
            _logger.Error($"ProcessMessageAsync task cancel to process message: {message} with exception: {ex.Message}");
            return (MessageProcessType.ConsumeErrorFatal, null, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"ProcessMessageAsync Connection error to process message: {message} with exception: {ex.Message}");
            return (MessageProcessType.Retain, null, null);
        }
        catch (Exception ex)
        {
            _logger.Error($"ProcessMessageAsync Failed to process message: {message} with exception: {ex.Message}");
            return (MessageProcessType.ConsumeErrorRecoverable, null, null);
        }

    }
}
