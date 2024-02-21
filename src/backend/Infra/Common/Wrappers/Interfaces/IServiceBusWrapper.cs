using Azure.Messaging.ServiceBus;

namespace Backend.Infra.Wrappers.Interfaces;

public interface IServiceBusWrapper
{
    ServiceBusClient CreateServiceBusClient(string queueName);
    ServiceBusSender CreateServiceBusSender(ServiceBusClient client, string queueName);
    Task SendMessageToQueue(ServiceBusSender sender, ServiceBusMessage serviceBusMessage);
}
