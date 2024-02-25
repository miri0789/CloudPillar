﻿using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;


namespace Backender.Wrappers.Interfaces;
public interface IServiceBusClientWrapper
{
    ServiceBusClient CreateServiceBusClient(string connectionString);

    ServiceBusAdministrationClient CreateServiceBusAdministrationClient(string connectionString);

    ServiceBusSender CreateSender(ServiceBusClient client, string queueName);

    ServiceBusProcessor CreateProcessor(ServiceBusClient client, string queueName, ServiceBusProcessorOptions options);

    ServiceBusProcessor CreateProcessor(ServiceBusClient client, string topicName, string subscriptionName, ServiceBusProcessorOptions options);

    ServiceBusProcessorOptions CreateProcessorOptions(int parallelCount, int maxLockDurationSeconds);

    Task StartProcessingAsync(ServiceBusProcessor processor, CancellationToken cancellationToken);

    Task StopProcessingAsync(ServiceBusProcessor processor);

    Task CompleteMessageAsync(ProcessMessageEventArgs processor);

    Task AbandonMessageAsync(ProcessMessageEventArgs processor);

    Task DisposeAsync(ServiceBusProcessor processor);

    void SetProcessorEvents(ServiceBusProcessor processor, Func<ProcessMessageEventArgs, Task> messageHandler, Func<ProcessErrorEventArgs, Task> errorHandler);

    Task<bool> QueueExistsAsync(ServiceBusAdministrationClient client, string queueName);

    Task CreateQueueAsync(ServiceBusAdministrationClient client, string queueName);

    Task<bool> SubscriptionExistsAsync(ServiceBusAdministrationClient client, string topicName, string subscriptionName);

    Task CreateSubscriptionAsync(ServiceBusAdministrationClient client, string topicName, string subscriptionName);

    Task<bool> TopicExistsAsync(ServiceBusAdministrationClient client, string topicName);

    Task CreateTopicAsync(ServiceBusAdministrationClient client, string topicName);
}