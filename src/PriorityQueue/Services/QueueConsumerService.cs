using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using PriorityQueue.Services.Interfaces;
using Shared.Logger;

namespace PriorityQueue.Services;
public class QueueConsumerService : BackgroundService
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly ILoggerHandler _logger;
    private readonly int _parallelCount;
    private readonly List<ServiceBusProcessor> _processors = new List<ServiceBusProcessor>();
    private readonly string _serviceBusConnectionString;
    private readonly List<string> _serviceBusUrls;
    private int _currentParallelCount = 0;
    private Dictionary<ServiceBusProcessor, Func<ProcessMessageEventArgs, Task>> _messageHandlers = new Dictionary<ServiceBusProcessor, Func<ProcessMessageEventArgs, Task>>();



    public QueueConsumerService(IMessageProcessor messageProcessor,
                                ILoggerHandler logger,
                                string serviceBusConnectionString,
                                List<string> serviceBusUrls,
                                int parallelCount)
    {
        _messageProcessor = messageProcessor;
        _serviceBusConnectionString = serviceBusConnectionString;
        _serviceBusUrls = serviceBusUrls;
        _parallelCount = parallelCount;
        _logger = logger;
    }



    ///  to do using ServiceBusClient
    private async Task InitializeProcessors(List<string> serviceBusUrls)
    {
        var client = new ServiceBusClient(_serviceBusConnectionString);
        ServiceBusAdministrationClient? adminClient = null;
        try
        { // Do I have permission?
            adminClient = new ServiceBusAdministrationClient(_serviceBusConnectionString);
        }
        catch (Exception)
        {
            //log!!
        }

        // Read max lock duration from environment variable
        int maxLockDurationSeconds = int.TryParse(Environment.GetEnvironmentVariable("MAX_LOCK_DURATION_SECONDS"), out int duration) ? duration : 30; // Default to 30 seconds if not set

        foreach (var url in serviceBusUrls)
        {
            ServiceBusProcessor processor;
            if (IsQueueUrl(url))
            {
                // Check and create queue if it doesn't exist
                if (adminClient != null && !await adminClient.QueueExistsAsync(url))
                {
                    await adminClient.CreateQueueAsync(url);
                }

                processor = client.CreateProcessor(url, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = _parallelCount, // Controlled at the service level
                    AutoCompleteMessages = false,
                    MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(maxLockDurationSeconds),
                    ReceiveMode = ServiceBusReceiveMode.PeekLock // Explicitly setting PeekLock
                });
            }
            else
            {
                var (topicName, subscriptionName) = ParseTopicSubscription(url);

                // Check and create topic if it doesn't exist
                if (adminClient != null && !await adminClient.TopicExistsAsync(topicName))
                {
                    await adminClient.CreateTopicAsync(topicName);
                }

                // Check and create subscription if it doesn't exist
                if (adminClient != null && !await adminClient.SubscriptionExistsAsync(topicName, subscriptionName))
                {
                    await adminClient.CreateSubscriptionAsync(topicName, subscriptionName);
                }

                processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = _parallelCount, // Controlled at the service level
                    AutoCompleteMessages = false,
                    MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(maxLockDurationSeconds),
                    ReceiveMode = ServiceBusReceiveMode.PeekLock // Explicitly setting PeekLock
                });
            }

            _processors.Add(processor);

            // The index of the newly added processor
            int processorIndex = _processors.Count - 1;
            Func<ProcessMessageEventArgs, Task> messageHandler = async (args) => await OnMessageReceived(args, processorIndex);
            _messageHandlers[processor] = messageHandler;
            processor.ProcessMessageAsync += messageHandler;

            processor.ProcessErrorAsync += OnProcessError; // Assign the error handler
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeProcessors(_serviceBusUrls);
        await ProcessInPriorityOrder(stoppingToken);
    }

    private async Task ProcessInPriorityOrder(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            for (int i = 0; i < _processors.Count && _currentParallelCount < _parallelCount; i++)
            {
                _logger.Debug($"{DateTime.Now} start proccess loop {i}");
                var processor = _processors[i];
                if (!processor.IsProcessing)
                {
                    await processor.StartProcessingAsync();

                    _logger.Debug($"{DateTime.Now} waiting for 2 seconds");
                    // Grace period for higher priority queues to receive messages
                    await Task.Delay(2000); // 2 seconds grace period for this priority
                }
                if (i == 0 && _processors.Count > 1 && !_processors[i].IsProcessing)
                {
                    _logger.Debug($"{DateTime.Now} first waiting for 2 seconds");
                    await Task.Delay(2000);
                }

                // If any messages are received and being processed, don't advance to lower priority
                if (_currentParallelCount > 0)
                {
                    _logger.Debug($"{DateTime.Now} _currentParallelCount > 0");
                    break;
                }
            }

            // If no messages are being processed in any queue, wait longer before re-checking
            if (_currentParallelCount == 0)
            {
                _logger.Debug($"{DateTime.Now} waiting for 5 seconds");
                await Task.Delay(5000, stoppingToken); // 5 seconds idle delay, stay with higher priority processors
            }
        }

        await StopLowerPriorityProcessorsAsync();
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args, int processorIndex)
    {
        await StopLowerPriorityProcessorsAsync(processorIndex);

        _logger.Debug($"{DateTime.Now} _currentParallelCount: {_currentParallelCount} before increment");
        if (Interlocked.Increment(ref _currentParallelCount) > _parallelCount)
        {
            _logger.Debug($"{DateTime.Now} Message {args.Message.Body} Decrement");
            Interlocked.Decrement(ref _currentParallelCount);
            _logger.Debug($"{DateTime.Now} _currentParallelCount: {_currentParallelCount} afetr decrement {args.Message.Body}");
            await args.AbandonMessageAsync(args.Message);
            return;
        }
        _logger.Debug($"{DateTime.Now} _currentParallelCount: {_currentParallelCount} afetr increment");

        try
        {
            if (await _messageProcessor.ProcessMessageAsync(args.Message.Body.ToString(), args.Message.ApplicationProperties.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty)))
            {
                // Distinguish failure types: is the service is the problem, abandon to let another service to process;
                // If its the message problem (HTTP 4xx) - no recovery, fatal failure, do not abandon, just fail
                await args.CompleteMessageAsync(args.Message);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _currentParallelCount);
        }
    }


    private async Task StopLowerPriorityProcessorsAsync(int currentIndex = -1)
    {
        foreach (var processor in _processors.Skip(currentIndex + 1).Where(p => p.IsProcessing))
        {
            await processor.StopProcessingAsync();
        }
    }

    private bool IsQueueUrl(string url)
    {
        // Assuming URLs not containing '/' are queues
        return !url.Contains("/");
    }

    private (string topicName, string subscriptionName) ParseTopicSubscription(string url)
    {
        var parts = url.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException("The URL must be in the format 'topicName/subscriptionName'.", nameof(url));
        }

        return (parts[0], parts[1]);
    }

    private Task OnProcessError(ProcessErrorEventArgs args)
    {
        // Error handling logic
        _logger.Debug($"Error occurred: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
