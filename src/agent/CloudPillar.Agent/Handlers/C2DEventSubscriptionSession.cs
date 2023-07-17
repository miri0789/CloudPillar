using System.Text;
using Microsoft.Azure.Devices.Client;
using shared.Entities.Messages;
using CloudPillar.Agent.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class C2DEventSubscriptionSession : IC2DEventSubscriptionSession
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    public C2DEventSubscriptionSession(IDeviceClientWrapper deviceClientWrapper, IFileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _deviceClientWrapper = deviceClientWrapper;
        _fileDownloadHandler = fileDownloadHandler;
    }


    public async Task ReceiveC2DMessagesAsync(DeviceClient deviceClient, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage;

            try
            {
                receivedMessage = await _deviceClientWrapper.ReceiveAsync(cancellationToken, deviceClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{0}: Exception hit when receiving the message, ignoring it: {1}", DateTime.Now, ex.Message);
                continue;
            }

            try
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                MessageType.TryParse(receivedMessage.Properties["MessageType"], out MessageType messageType);
                IMessageSubscriber subscriber = null;
                BaseMessage message = null;
                switch (messageType)
                {
                    case MessageType.DownloadChunk:
                        message = new DownloadBlobChunkMessage(receivedMessage);
                        subscriber = _fileDownloadHandler;
                        break;
                    default: break;
                }
                if (subscriber != null)
                {
                    await subscriber.HandleMessage(message);
                }

                await _deviceClientWrapper.CompleteAsync(receivedMessage, deviceClient);
                Console.WriteLine($"{0}: Recived message of type: {1} completed", DateTime.Now, messageType.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{0}: Exception hit when parsing the message, ignoring it: {1}", DateTime.Now, ex.Message);
                continue;
            }
        }
    }

    
}