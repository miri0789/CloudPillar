using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Utilities.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IMessageFactory _messageFactory;
    private readonly IStateMachineHandler _stateMachineHandler;
    private readonly ICheckExceptionResult _checkExceptionResult;
    private readonly ILoggerHandler _logger;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
                                       IMessageSubscriber messageSubscriber,
                                       IMessageFactory messageFactory,
                                       IStateMachineHandler stateMachineHandler,
                                    ICheckExceptionResult checkExceptionResult,
                                       ILoggerHandler logger)
    {
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _messageSubscriber = messageSubscriber ?? throw new ArgumentNullException(nameof(messageSubscriber));
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        _checkExceptionResult = checkExceptionResult ?? throw new ArgumentNullException(nameof(checkExceptionResult));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task ReceiveC2DMessagesAsync(CancellationToken cancellationToken, bool isProvisioning)
    {
        _logger.Info("Subscribing to C2D messages...");
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
                _logger.Error($"Certificate expired, ignoring it message: {ex.Message}");
                var errorCode = _checkExceptionResult.IsDeviceConnectException(ex.Message);
                if (errorCode != null)
                {
                    break;
                }
                continue;
            }
            var parseMessage = Enum.TryParse(receivedMessage.Properties[MESSAGE_TYPE_PROP], out C2DMessageType messageType);
            try
            {
                if (parseMessage)
                {
                    _logger.Info($"Receive message of type: {receivedMessage.Properties[MESSAGE_TYPE_PROP]}");
                    var isHandleMsg = !isProvisioning && await HandleMessage(receivedMessage, cancellationToken, messageType);
                    if (!isHandleMsg)
                    {
                        await HandleProvisioningMessage(receivedMessage, cancellationToken, messageType);
                    }
                }
                else
                {
                    _logger.Error($"Unknown recived message type");
                }

            }
            catch (Exception ex)
            {
                _logger.Error($"Exception hit when parsing the message, ignoring it message: {ex.Message}");
                continue;
            }
            finally
            {
                if (messageType != C2DMessageType.Reprovisioning)
                {
                    try
                    {
                        await _deviceClient.CompleteAsync(receivedMessage, cancellationToken);
                        _logger.Info($"Receive message of type: {receivedMessage.Properties[MESSAGE_TYPE_PROP]} completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Complete message of type: {receivedMessage.Properties[MESSAGE_TYPE_PROP]} failed message {ex.Message}");
                    }
                }
            }
        }
    }

    private async Task HandleProvisioningMessage(Message receivedMessage, CancellationToken cancellationToken, C2DMessageType? messageType)
    {
        switch (messageType)
        {
            case C2DMessageType.Reprovisioning:
                var reprovisioningMessage = _messageFactory.CreateC2DMessageFromMessage<ReprovisioningMessage>(receivedMessage);
                await _messageSubscriber.HandleReprovisioningMessageAsync(receivedMessage, reprovisioningMessage, cancellationToken);
                await _stateMachineHandler.SetStateAsync(DeviceStateType.Ready, cancellationToken, true);
                break;
            case C2DMessageType.RequestDeviceCertificate:
                var requestDeviceCertificateMessage = _messageFactory.CreateC2DMessageFromMessage<RequestDeviceCertificateMessage>(receivedMessage);
                await _messageSubscriber.HandleRequestDeviceCertificateAsync(requestDeviceCertificateMessage, cancellationToken);
                break;
            default:
                _logger.Warn($"Receive message was not processed type: {messageType?.ToString()} , provisioning mode");
                break;
        }

    }

    private async Task<bool> HandleMessage(Message receivedMessage, CancellationToken cancellationToken, C2DMessageType? messageType)
    {
        switch (messageType)
        {
            case C2DMessageType.DownloadChunk:
                var message = _messageFactory.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                await _messageSubscriber.HandleDownloadMessageAsync(message, cancellationToken);
                return true;
            default:
                _logger.Warn($"Receive message was not processed type: {messageType?.ToString()}");
                break;
        }
        return false;
    }
}