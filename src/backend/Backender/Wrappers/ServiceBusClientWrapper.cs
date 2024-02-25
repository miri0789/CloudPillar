using Backender.Wrappers.Interfaces;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Backender.Wrappers;
public class ServiceBusClientWrapper : IServiceBusClientWrapper
{
    public ServiceBusClient CreateServiceBusClient(string connectionString)
    {
        return new ServiceBusClient(connectionString);
    }

    public ServiceBusAdministrationClient CreateServiceBusAdministrationClient(string connectionString)
    {
        return new ServiceBusAdministrationClient(connectionString);
    }

    public ServiceBusSender CreateSender(ServiceBusClient client, string queueName)
    {
        return client.CreateSender(queueName);
    }

    public ServiceBusProcessor CreateProcessor(ServiceBusClient client, string queueName, ServiceBusProcessorOptions options)
    {
        return client.CreateProcessor(queueName, options);
    }

    public ServiceBusProcessor CreateProcessor(ServiceBusClient client, string topicName, string subscriptionName, ServiceBusProcessorOptions options)
    {
        return client.CreateProcessor(topicName, subscriptionName, options);
    }

    public ServiceBusProcessorOptions CreateProcessorOptions(int parallelCount, int maxLockDurationSeconds)
    {
        return new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = parallelCount,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(maxLockDurationSeconds),
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        };
    }

    public async Task< bool> QueueExistsAsync(ServiceBusAdministrationClient client, string queueName)
    {
        return await client.QueueExistsAsync(queueName);
    }

    public async Task StartProcessingAsync(ServiceBusProcessor processor, CancellationToken cancellationToken)
    {
        await processor.StartProcessingAsync(cancellationToken);
    }

    public async Task StopProcessingAsync(ServiceBusProcessor processor)
    {
        await processor.StopProcessingAsync();
    }

    public async Task CompleteMessageAsync(ProcessMessageEventArgs processor, ServiceBusReceivedMessage message)
    {
        await processor.CompleteMessageAsync(message);
    }

    public async Task AbandonMessageAsync(ProcessMessageEventArgs processor, ServiceBusReceivedMessage message)
    {
        await processor.AbandonMessageAsync(message);
    }

    public async Task DisposeAsync(ServiceBusProcessor processor)
    {
        await processor.DisposeAsync();
    }

    public void SetProcessorEvents(ServiceBusProcessor processor, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler)
    {
        processor.ProcessMessageAsync += messageHandler;
        processor.ProcessErrorAsync += errorHandler;
    }
}
