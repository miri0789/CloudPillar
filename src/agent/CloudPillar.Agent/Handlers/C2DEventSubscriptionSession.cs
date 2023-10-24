using System.Text;
using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Shared.Logger;
using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IMessageFactory _messageFactory;
    private readonly ITwinActionsHandler _twinActionsHandler;

    private readonly ILoggerHandler _logger;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
                                       IMessageSubscriber messageSubscriber,
                                       IMessageFactory messageFactory,
                                       ITwinActionsHandler twinActionsHandler,
                                       ILoggerHandler logger)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _messageSubscriber = messageSubscriber ?? throw new ArgumentNullException(nameof(messageSubscriber));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task<bool> ReceiveC2DMessagesAsync(CancellationToken cancellationToken, bool isProvisioning)
    {
        const string MESSAGE_TYPE_PROP = "MessageType";
        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage;

            try
            {
                receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception hit when receiving the message, ignoring it", ex);
                continue;
            }

            try
            {
                if (Enum.TryParse(receivedMessage.Properties[MESSAGE_TYPE_PROP], out C2DMessageType messageType))
                {
                    if (isProvisioning)
                    {
                        var isFinishRecived = await HandleProvisioningMessage(receivedMessage, cancellationToken, messageType);
                        if (isFinishRecived)
                        {
                            return isFinishRecived;
                        }
                    }
                    else
                    {
                        await HandleMessage(receivedMessage, cancellationToken, messageType);
                    }
                }
                else
                {
                    _logger.Error($"Unknown recived message type");
                }

            }
            catch (Exception ex)
            {
                _logger.Error("Exception hit when parsing the message, ignoring it", ex);
                continue;
            }
            finally
            {
                await _deviceClient.CompleteAsync(receivedMessage);
                _logger.Info($"Receive message of type: {receivedMessage.Properties[MESSAGE_TYPE_PROP]} completed");
            }
        }
        return false;
    }

    private async Task<bool> HandleProvisioningMessage(Message receivedMessage, CancellationToken cancellationToken, C2DMessageType? messageType)
    {
        var isReprovisioning = false;
        switch (messageType)
        {
            case C2DMessageType.Reprovisioning:
                var reprovisioningMessage = _messageFactory.CreateC2DMessageFromMessage<ReprovisioningMessage>(receivedMessage);
                isReprovisioning = await _messageSubscriber.HandleReprovisioningMessageAsync(reprovisioningMessage, cancellationToken);
                break;
            case C2DMessageType.RequestDeviceCertificate:
                var requestDeviceCertificateMessage = _messageFactory.CreateC2DMessageFromMessage<RequestDeviceCertificateMessage>(receivedMessage);
                await _messageSubscriber.HandleRequestDeviceCertificateAsync(requestDeviceCertificateMessage, cancellationToken);
                break;
            default:
                _logger.Warn($"Receive message was not processed type: {messageType?.ToString()} , provisioning mode");
                break;
        }

        return isReprovisioning;
    }

    private async Task HandleMessage(Message receivedMessage, CancellationToken cancellationToken, C2DMessageType? messageType)
    {
        switch (messageType)
        {
            case C2DMessageType.DownloadChunk:
                var message = _messageFactory.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                var actionToReport = await _messageSubscriber.HandleDownloadMessageAsync(message);
                await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
                break;
            default:
                _logger.Warn($"Receive  message was not processed type: {messageType?.ToString()}");
                break;
        }
    }


}