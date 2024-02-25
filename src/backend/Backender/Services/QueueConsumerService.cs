using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Backender.Services.Interfaces;
using Backender.Wrappers.Interfaces;
using Shared.Logger;
using Backender.Entities.Enums;
using Backender.Entities;
using System.Text;

namespace Backender.Services;
public class QueueConsumerService : BackgroundService
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly ILoggerHandler _logger;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IServiceBusClientWrapper _serviceBusClientWrapper;    
    private readonly IList<ServiceBusProcessor> _processors;
    private static int _currentParallelCount = 0;
    private ServiceBusClient _client;

    private const string QUEUE_INDICATOR = "/";


    public QueueConsumerService(IMessageProcessor messageProcessor,
                                ILoggerHandler logger,
                                IEnvironmentsWrapper environmentsWrapper,
                                IServiceBusClientWrapper serviceBusClientWrapper)
    {
        _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceBusClientWrapper = serviceBusClientWrapper ?? throw new ArgumentNullException(nameof(serviceBusClientWrapper));
        if (string.IsNullOrWhiteSpace(_environmentsWrapper.ServiceBusConnectionString))
        {
            throw new ArgumentNullException(nameof(_environmentsWrapper.ServiceBusConnectionString));
        }
        _client = _serviceBusClientWrapper.CreateServiceBusClient(_environmentsWrapper.ServiceBusConnectionString);
        _processors = new List<ServiceBusProcessor>();
    }


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await InitializeProcessorsAsync(_environmentsWrapper.ServiceBusUrls, cancellationToken);
        await ProcessInPriorityOrderAsync(cancellationToken);
    }

    private async Task InitializeProcessorsAsync(string[] serviceBusUrls, CancellationToken cancellationToken)
    {
        var adminClient = CreateAdminClient();

        foreach (var url in serviceBusUrls)
        {
            var processor = await CreateProcessorAsync(adminClient, url);
            SubscribeToProcessorEvents(processor, cancellationToken);
            _processors.Add(processor);
        }
    }

    private ServiceBusAdministrationClient? CreateAdminClient()
    {
        try
        {
            return _serviceBusClientWrapper.CreateServiceBusAdministrationClient(_environmentsWrapper.ServiceBusConnectionString);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to create ServiceBusAdministrationClient: {ex.Message}");
            return null;
        }
    }

    private async Task<ServiceBusProcessor> CreateProcessorAsync(ServiceBusAdministrationClient? adminClient, string url)
    {
        var processorOptions = _serviceBusClientWrapper.CreateProcessorOptions(_environmentsWrapper.ParallelCount, _environmentsWrapper.MaxLockDurationSeconds);
        if (IsQueueUrl(url))
        {
            await EnsureQueueExistsAsync(adminClient, url);
            return _serviceBusClientWrapper.CreateProcessor(_client, url, processorOptions);
        }
        else
        {
            var (topicName, subscriptionName) = ParseTopicSubscription(url);
            await EnsureTopicAndSubscriptionExistAsync(adminClient, topicName, subscriptionName);
            return _serviceBusClientWrapper.CreateProcessor(_client, topicName, subscriptionName, processorOptions);
        }

    }

    private async Task EnsureQueueExistsAsync(ServiceBusAdministrationClient? adminClient, string queueUrl)
    {
        if (adminClient != null && !await adminClient.QueueExistsAsync(queueUrl))
        {
            await adminClient.CreateQueueAsync(queueUrl);
        }
    }

    private async Task EnsureTopicAndSubscriptionExistAsync(ServiceBusAdministrationClient? adminClient, string topicName, string subscriptionName)
    {
        if (adminClient != null && !await adminClient.TopicExistsAsync(topicName))
        {
            await adminClient.CreateTopicAsync(topicName);
        }

        if (adminClient != null && !await adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
        {
            await adminClient.CreateSubscriptionAsync(topicName, subscriptionName);
        }
    }

    private void SubscribeToProcessorEvents(ServiceBusProcessor processor, CancellationToken cancellationToken)
    {
        int processorIndex = _processors.Count;

        Func<ProcessMessageEventArgs, Task> messageHandler = async (args) => await OnMessageReceivedAsync(args, processorIndex, cancellationToken);
        _serviceBusClientWrapper.SetProcessorEvents(processor, messageHandler, OnProcessError);
    }

    private bool IsQueueUrl(string url)
    {
        return !url.Contains(QUEUE_INDICATOR);
    }

    private (string topicName, string subscriptionName) ParseTopicSubscription(string url)
    {
        var parts = url.Split(QUEUE_INDICATOR);
        if (parts.Length != 2)
        {
            throw new ArgumentException("The URL must be in the format 'topicName/subscriptionName'.", nameof(url));
        }

        return (parts[0], parts[1]);
    }



    private async Task ProcessInPriorityOrderAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            for (int i = 0; i < _processors.Count && _currentParallelCount < _environmentsWrapper.ParallelCount; i++)
            {
                var processor = _processors[i];
                var isQueueProcessing = processor.IsProcessing;

                if (!isQueueProcessing && i != 0)
                {
                    _logger.Info($"Grace period for higher priority queues to receive messages, queue index {i}");
                    await Task.Delay(_environmentsWrapper.HigherPriorityGraceMS, cancellationToken);

                    // If any messages are received and being processed, don't advance to lower priority
                    if (_currentParallelCount >= _environmentsWrapper.ParallelCount)
                    {
                        _logger.Debug($"Any messages are received and being processed, don't advance to lower priority, queue index {i}");
                        break;
                    }
                }

                if (!isQueueProcessing && !cancellationToken.IsCancellationRequested)
                {
                    _logger.Info($"Starting processor for queue {processor.EntityPath}");
                    await _serviceBusClientWrapper.StartProcessingAsync(processor, cancellationToken);
                }
            }

            // If no messages are being processed in any queue, wait longer before re-checking
            if (_currentParallelCount == 0 && !cancellationToken.IsCancellationRequested)
            {
                _logger.Debug("No messages are being processed in any queue, wait longer before re-checking");
                await Task.Delay(_environmentsWrapper.NoMessagesDelayMS, cancellationToken);
            }
        }

        await DisposeProcessorsAsync();
    }

    private async Task DisposeProcessorsAsync()
    {
        foreach (var processor in _processors)
        {
            await _serviceBusClientWrapper.DisposeAsync(processor);
        }
    }


    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args, int processorIndex, CancellationToken cancellationToken)
    {
        await StopLowerPriorityProcessorsAsync(processorIndex);

        if (Interlocked.Increment(ref _currentParallelCount) > _environmentsWrapper.ParallelCount)
        {
            Interlocked.Decrement(ref _currentParallelCount);
            await args.AbandonMessageAsync(args.Message);
            _logger.Info($"OnMessageReceivedAsync AbandonMessageAsync _currentParallelCount > parallelCount , queue index {processorIndex}, msg id: {args.Message.CorrelationId}");
            return;
        }

        try
        {
            var properties = args.Message.ApplicationProperties.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);
            (CompletionCode type, string response, IDictionary<string, string>? responseHeaers)
             = await _messageProcessor.ProcessMessageAsync(args.Message.Body.ToString(), properties, cancellationToken);
            if (type == CompletionCode.Retain)
            {
                await args.AbandonMessageAsync(args.Message);
                _logger.Info($"OnMessageReceivedAsync ProcessMessageAsync Retain AbandonMessageAsync, queue index {processorIndex}, msg id: {args.Message.CorrelationId}");
            }
            else
            {
                var relativeUri = GetCompleteRelatevieUri(type, properties);

                await SendCompleteTopic(response, relativeUri, responseHeaers, cancellationToken);
                await SendCompleteRequestAsync(response, relativeUri, responseHeaers, cancellationToken);

                await args.CompleteMessageAsync(args.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"OnMessageReceivedAsync ProcessMessageAsync failed: {ex.Message}");
            await args.AbandonMessageAsync(args.Message);
        }
        finally
        {
            Interlocked.Decrement(ref _currentParallelCount);
        }
    }


    private string GetCompleteRelatevieUri(CompletionCode type, IDictionary<string, string> properties)
    {
        var relativeUri = properties.TryGetValue(Constants.COMPLETION_RELATIVR_URI, out var uri) ? uri : "";

        if (!string.IsNullOrWhiteSpace(relativeUri))
        {
            return $"{relativeUri}&resultCode={type}";
        }
        else
        {
            return $"?resultCode={type}";
        }
    }

    private async Task SendCompleteRequestAsync(string response, string relativeUri, IDictionary<string, string>? responseHeaers, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_environmentsWrapper.CompletionUrlBase))
        {
            try
            {
                await _messageProcessor.SendPostRequestAsync($"{_environmentsWrapper.CompletionUrlBase}{relativeUri}", response, responseHeaers, cancellationToken);
                _logger.Info($"SendCompleteRequestAsync for uri {relativeUri} success");

            }
            catch (Exception ex)
            {
                _logger.Error($"OnMessageReceivedAsync ProcessMessageAsync SendPostRequestAsync failed: {ex.Message}");
            }
        }
    }
    private async Task SendCompleteTopic(string response, string relativeUri, IDictionary<string, string>? responseHeaers, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_environmentsWrapper.CompletionTopic))
        {
            try
            {
                var completionTopicSender = _serviceBusClientWrapper.CreateSender(_client, _environmentsWrapper.CompletionTopic);
                var completionMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(response));
                responseHeaers?.ToList().ForEach(h => completionMessage.ApplicationProperties.Add(h.Key, h.Value));
                completionMessage.ApplicationProperties.Add(Constants.RELATIVE_URI_PROP, relativeUri);

                await completionTopicSender.SendMessageAsync(completionMessage, cancellationToken);
                _logger.Info($"SendCompleteTopic for uri {relativeUri} succeeded");
            }
            catch (Exception ex)
            {
                _logger.Error($"SendCompleteTopic for uri {relativeUri} failed: {ex.Message}");
            }
        }
    }


    private async Task StopLowerPriorityProcessorsAsync(int currentIndex)
    {
        foreach (var processor in _processors.Skip(currentIndex + 1).Where(p => p.IsProcessing))
        {
            _logger.Debug($"Stopping processor for queue {processor.EntityPath}");
            await _serviceBusClientWrapper.StopProcessingAsync(processor);
        }
    }


    private Task OnProcessError(ProcessErrorEventArgs args)
    {
        // Error handling logic
        _logger.Error($"Error occurred: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
