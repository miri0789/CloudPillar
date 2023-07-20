using System.Text;
using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IMessagesFactory _messagesFactory;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper,
    IFileDownloadHandler fileDownloadHandler, IMessagesFactory messagesFactory)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);
        ArgumentNullException.ThrowIfNull(messagesFactory);

        _messagesFactory = messagesFactory;
        _deviceClientWrapper = deviceClientWrapper;
        _fileDownloadHandler = fileDownloadHandler;
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
                IMessageSubscriber subscriber = null;
                BaseMessage message = null;
                switch (messageType)
                {
                    case MessageType.DownloadChunk:
                        message = _messagesFactory.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                        subscriber = _fileDownloadHandler;
                        break;
                    default: 
                        Console.WriteLine($"Recived message was not processed");
                        break;
                }
                if (subscriber != null)
                {
                    await subscriber.HandleMessageAsync(message);
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