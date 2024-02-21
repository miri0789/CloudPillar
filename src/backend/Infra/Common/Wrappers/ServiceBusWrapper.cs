using Azure.Messaging.ServiceBus;
using Backend.Infra.Wrappers.Interfaces;

namespace Backend.Infra.Wrappers;

public class ServiceBusWrapper : IServiceBusWrapper
{

    public ServiceBusClient CreateServiceBusClient(string serviceBusConnectionString)
    {
        return new ServiceBusClient(serviceBusConnectionString);
    }

    public ServiceBusSender CreateServiceBusSender(ServiceBusClient client, string queueName)
    {
        return client.CreateSender(queueName);
    }

    public async Task SendMessageToQueue(ServiceBusSender sender, ServiceBusMessage serviceBusMessage)
    {
        await sender.SendMessageAsync(serviceBusMessage);
    }
}