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
    private readonly IMessageFactory _messageFactory;
    private readonly ITwinHandler _twinHandler;

    private readonly ILoggerHandler _logger;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
                                       IMessageSubscriber messageSubscriber,
                                       IMessageFactory messageFactory,
                                       ITwinHandler twinHandler,
                                       ILoggerHandler logger)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
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
                C2DMessageType? messageType = null;
                if (Enum.TryParse(receivedMessage.Properties[messageTypeProp], out C2DMessageType parsedMessageType))
                {
                    messageType = parsedMessageType;
                }
                switch (messageType)
                {
                    case C2DMessageType.DownloadChunk:
                        var message = _messageFactory.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                        var actionToReport = await _messageSubscriber.HandleDownloadMessageAsync(message);
                        await _twinHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1));
                        break;
                    case C2DMessageType.ReProvisioning:
                        var reProvisioningMessage = _messageFactory.CreateC2DMessageFromMessage<ReProvisioningMessage>(receivedMessage);
                        await _messageSubscriber.HandleReProvisioningMessageAsync(reProvisioningMessage, cancellationToken);

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