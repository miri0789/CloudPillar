using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Hosting;
using PriorityQueue.Services.Interfaces;
using System.Text;
using Shared.Logger;

namespace PriorityQueue.Services;
public class InsertMsges : BackgroundService
{


    private readonly IMessageProcessor _messageProcessor;
    private readonly int _parallelCount;
    private readonly ILoggerHandler _logger;
    private readonly List<ServiceBusProcessor> _processors = new List<ServiceBusProcessor>();
    private readonly string _serviceBusConnectionString;
    private readonly List<string> _serviceBusUrls;
    private int _currentParallelCount = 0;


    public InsertMsges(IMessageProcessor messageProcessor, ILoggerHandler logger, string serviceBusConnectionString, List<string> serviceBusUrls, int parallelCount)
    {
        _messageProcessor = messageProcessor;
        _serviceBusConnectionString = serviceBusConnectionString;
        _serviceBusUrls = serviceBusUrls;
        _parallelCount = parallelCount;
        _logger = logger;
    }



    private async Task InitializeProcessors(List<string> serviceBusUrls)
    {


        await using var client = new ServiceBusClient(_serviceBusConnectionString);
        
        for (int i = 0; i < 100; i++)
        {
            // Choose a random queue
            var randomQueueName = serviceBusUrls[new Random().Next(serviceBusUrls.Count)];

            // Create a sender for the randomly chosen queue
            var sender = client.CreateSender(randomQueueName);

            // Create and send a message
            var messageBody = $"Hello, Service Bus! Message {i + 1} to {randomQueueName}";
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody));

            await sender.SendMessageAsync(message);

            _logger.Debug($"{DateTime.Now} Message {i + 1} sent to {randomQueueName}: {messageBody}");

            // Close the sender
            await sender.CloseAsync();
        }
        await Task.Delay(40000);
        var sender1 = client.CreateSender(serviceBusUrls.First());

        // Create and send a message
        var messageBody1 = $"Hello, Service Bus! Message xxxxxxxxxxxx to {serviceBusUrls.First()}";
        var message1 = new ServiceBusMessage(Encoding.UTF8.GetBytes(messageBody1));

        await sender1.SendMessageAsync(message1);

        _logger.Debug($"Message xxxxxxxxxxxx sent to {serviceBusUrls.First()}: {messageBody1}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeProcessors(_serviceBusUrls);
    }

}
