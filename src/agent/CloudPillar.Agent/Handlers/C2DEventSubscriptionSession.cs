using System.Text;
using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IMessageSubscriber _messageSubscriber;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IMessagesFactory _messagesFactory;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
                                       IMessageSubscriber messageSubscriber, 
                                       IMessagesFactory messagesFactory)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(messageSubscriber);
        ArgumentNullException.ThrowIfNull(messagesFactory);

        _messagesFactory = messagesFactory;
        _deviceClientWrapper = deviceClientWrapper;
        _messageSubscriber = messageSubscriber;
    }


    public async Task ReceiveC2DMessagesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage;

            try
            {
                receivedMessage = await _deviceClientWrapper.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Exception hit when receiving the message, ignoring it: {ex.Message}");
                continue;
            }

            try
            {
                const string messageTypeProp = "MessageType";
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                MessageType.TryParse(receivedMessage.Properties[messageTypeProp], out MessageType messageType);
                switch (messageType)
                {
                    case MessageType.DownloadChunk:
                        var message = _messagesFactory.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                        await _messageSubscriber.HandleDownloadMessageAsync(message);
                        break;
                    default: 
                        Console.WriteLine($"Recived message was not processed");
                        break;
                }

                await _deviceClientWrapper.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}: Recived message of type: {messageType.ToString()} completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Exception hit when parsing the message, ignoring it: {ex.Message}");
                continue;
            }
        }
    }


}