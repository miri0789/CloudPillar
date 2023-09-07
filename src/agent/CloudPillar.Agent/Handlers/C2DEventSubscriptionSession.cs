using System.Text;
using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IMessageFactory _MessageFactory;
    private readonly ITwinHandler _twinHandler;

    private readonly ILoggerHandler _logger;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
                                       IMessageSubscriber messageSubscriber,
                                       IMessageFactory MessageFactory,
                                       ITwinHandler twinHandler,
                                       ILoggerHandler logger)
    {
        _MessageFactory = MessageFactory ?? throw new ArgumentNullException(nameof(MessageFactory));
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _messageSubscriber = messageSubscriber ?? throw new ArgumentNullException(nameof(messageSubscriber));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task ReceiveC2DMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage;

            try
            {
                receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"{DateTime.Now}: Exception hit when receiving the message, ignoring it: {ex.Message}");
                continue;
            }

            try
            {
                const string messageTypeProp = "MessageType";
                MessageType? messageType = null;
                if (Enum.TryParse(receivedMessage.Properties[messageTypeProp], out MessageType parsedMessageType))
                {
                    messageType = parsedMessageType;
                }
                switch (messageType)
                {
                    case MessageType.DownloadChunk:
                        var message = _MessageFactory.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                        var actionToReport = await _messageSubscriber.HandleDownloadMessageAsync(message);
                        await _twinHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1));
                        break;
                    default:
                        _logger.Info("Receive  message was not processed");
                        break;
                }
                _logger.Info($"{DateTime.Now}: Receive message of type: {messageType.ToString()} completed");
            }
            catch (Exception ex)
            {
                _logger.Error($"{DateTime.Now}: Exception hit when parsing the message, ignoring it: {ex.Message}");
                continue;
            }
            finally
            {
                await _deviceClient.CompleteAsync(receivedMessage);
            }
        }
    }


}