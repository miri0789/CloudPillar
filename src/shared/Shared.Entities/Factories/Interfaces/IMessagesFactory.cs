using Microsoft.Azure.Devices;
using Shared.Entities.Messages;

namespace Shared.Entities.Factories;


public interface IMessagesFactory
{
    T CreateBaseMessageFromMessage<T>(Microsoft.Azure.Devices.Client.Message message) where T : BaseMessage, new();
    Message PrepareBlobMessage(BaseMessage baseMessage, int expiredMinutes = 60);
}

