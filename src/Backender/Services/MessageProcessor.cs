﻿using System.Text;
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

    public async Task<HttpResponseMessage > SendPostRequestAsync(string relativeUri, string body, IDictionary<string, string> headers, CancellationToken cts)
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(_environmentsWrapper.svcBackendUrl);
            foreach (var property in headers.Where(p => p.Key != RELATIVE_URI_PROP))
            {
                client.DefaultRequestHeaders.Add(property.Key, property.Value);
            }
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(relativeUri, content, cts);
            return response;

        }
    }

    public async Task<(MessageProcessType type, string? response, IDictionary<string, string>? responseHeaers)>
    ProcessMessageAsync(string message, IDictionary<string, string> properties, CancellationToken stoppingToken)
    {
        try
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken,
                new CancellationTokenSource(new TimeSpan(0, 0, _environmentsWrapper.requestTimeoutSeconds)).Token))
            {
                var relativeUri = properties.TryGetValue(RELATIVE_URI_PROP, out var uri) ? uri : "";
                _logger.Info($"ProcessMessageAsync Sending message: {message} to url: {relativeUri}");
                var response = await SendPostRequestAsync(relativeUri, message, properties, cts.Token);

                var responseContent = await response.Content.ReadAsStringAsync();
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