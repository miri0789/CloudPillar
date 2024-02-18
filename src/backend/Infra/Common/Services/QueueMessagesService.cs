using Azure.Messaging.ServiceBus;
using Backend.Infra.Common.Services.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Messages;
using Shared.Entities.QueueMessages;
using Shared.Logger;

namespace Backend.Infra.Common.Services;

public class QueueMessagesService : IQueueMessagesService
{
    private ILoggerHandler _logger;
    private const string SERVICE_BUS_CONNECTION_STRING = "Endpoint=sb://cpyaelsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OL0VzYmJ4XAVctHje5snF93wpKdWf1H9w+ASbL2UW6w=";
    private const string QUEUE_NAME = "cp-yael-sb-q";

    public QueueMessagesService(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // GetMessageFromQueue();
    }

    public async Task SendMessageToQueue(string message)
    {
        try
        {
            var client = new ServiceBusClient(SERVICE_BUS_CONNECTION_STRING);

            await using (ServiceBusSender sender = client.CreateSender(QUEUE_NAME))
            {
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(message);
                await sender.SendMessageAsync(serviceBusMessage);
                _logger.Info("Sent a single message to the queue: " + QUEUE_NAME);
            }

            await client.DisposeAsync();

        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
        }
    }

    public async Task GetMessageFromQueue()
    {
        var client = new ServiceBusClient(SERVICE_BUS_CONNECTION_STRING);

        await using (ServiceBusReceiver receiver = client.CreateReceiver(QUEUE_NAME))
        {
            while (true)
            {
                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                var messageBody = JsonConvert.SerializeObject(message.Body);
                var m = JsonConvert.DeserializeObject<FileDownloadMessage>(messageBody);
                await receiver.CompleteMessageAsync(message);
            }
        }

        await client.DisposeAsync();
    }
}