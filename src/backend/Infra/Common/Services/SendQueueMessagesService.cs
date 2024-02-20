using Azure.Messaging.ServiceBus;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Logger;

namespace Backend.Infra.Common.Services;

public class SendQueueMessagesService : ISendQueueMessagesService
{
    private ICommonEnvironmentsWrapper _environmentsWrapper;
    private ILoggerHandler _logger;

    public SendQueueMessagesService(ICommonEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendMessageToQueue(string message)
    {
        try
        {
            var client = new ServiceBusClient(_environmentsWrapper.serviceBusConnectionString);

            await using (ServiceBusSender sender = client.CreateSender(_environmentsWrapper.queueName))
            {
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(message);
                await sender.SendMessageAsync(serviceBusMessage);
                _logger.Info("Sent a single message to the queue: " + _environmentsWrapper.queueName);
            }

            await client.DisposeAsync();

        }
        catch (Exception e)
        {
            _logger.Error(e.Message);
        }
    }
}