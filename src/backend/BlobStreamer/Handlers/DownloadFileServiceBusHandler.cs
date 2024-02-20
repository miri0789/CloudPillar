using Azure.Messaging.ServiceBus;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.BlobStreamer.Services.Interfaces;
using System.Text;
using Newtonsoft.Json.Linq;
using Shared.Entities.QueueMessages;
using Newtonsoft.Json;
using Shared.Logger;
using Backend.BlobStreamer.Handlers.Interfaces;

namespace Backend.BlobStreamer.Handlers;

public class DownloadFileServiceBusHandler : IDownloadFileServiceBusHandler
{
    private ServiceBusClient client;
    private ServiceBusProcessor processor;
    private ICommonEnvironmentsWrapper _environmentsWrapper;
    private IBlobService _blobService;
    private ILoggerHandler _logger;

    public DownloadFileServiceBusHandler(ICommonEnvironmentsWrapper environmentsWrapper, IBlobService blobService, ILoggerHandler logger)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartProcessingAsync()
    {
        _logger.Info("Starting to process messages from the queue: " + _environmentsWrapper.queueName);
        client = new ServiceBusClient(_environmentsWrapper.serviceBusConnectionString);
        processor = client.CreateProcessor(_environmentsWrapper.queueName, new ServiceBusProcessorOptions());
        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;
        await processor.StartProcessingAsync();
    }

    public async Task StopProcessingAsync()
    {
        _logger.Info("Stopping to process messages from the queue: " + _environmentsWrapper.queueName);
        await processor.StopProcessingAsync();
        await processor.DisposeAsync();
        await client.DisposeAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        _logger.Info("Received a message from the queue: " + _environmentsWrapper.queueName);
        var body = args.Message.Body;
        Console.WriteLine($"Received: {body}");
        var messageString = Encoding.UTF8.GetString(body);
        JObject jsonObject = JObject.Parse(messageString);
        int messageType = (int)jsonObject["MessageType"];
        switch (messageType)
        {
            case (int)QueueMessageType.FileDownloadReady:
                var FileMessageBody = JsonConvert.DeserializeObject<FileDownloadQueueMessage>(messageString);
                await _blobService.SendFileDownloadAsync(FileMessageBody);
                break;
            case (int)QueueMessageType.SendRangeByChunks:
                var RangeMessageBody = JsonConvert.DeserializeObject<SendRangeByChunksQueueMessage>(messageString);
                await _blobService.SendRangeByChunksAsync(RangeMessageBody);
                break;
        }
        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.Error(args.Exception.ToString());
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }
}