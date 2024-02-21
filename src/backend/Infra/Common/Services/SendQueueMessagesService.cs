using Azure.Messaging.ServiceBus;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Logger;

namespace Backend.Infra.Common.Services;

public class SendQueueMessagesService : ISendQueueMessagesService
{
    private ICommonEnvironmentsWrapper _environmentsWrapper;
    private IServiceBusWrapper _serviceBusWrapper;
    private ILoggerHandler _logger;

    public SendQueueMessagesService(ICommonEnvironmentsWrapper environmentsWrapper, IServiceBusWrapper serviceBusWrapper, ILoggerHandler logger)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _serviceBusWrapper = serviceBusWrapper ?? throw new ArgumentNullException(nameof(serviceBusWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendMessageToQueue(string url, object message = null)
    {
        try
        {
            var client = _serviceBusWrapper.CreateServiceBusClient(_environmentsWrapper.serviceBusConnectionString);

            await using (ServiceBusSender sender = _serviceBusWrapper.CreateServiceBusSender(client, _environmentsWrapper.queueName))
            {
                var stringMessage = JsonConvert.SerializeObject(message);
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(stringMessage);
                serviceBusMessage.ApplicationProperties.Add(CommonConstants.RELATIVE_URI, url);
                await _serviceBusWrapper.SendMessageToQueue(sender, serviceBusMessage);
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