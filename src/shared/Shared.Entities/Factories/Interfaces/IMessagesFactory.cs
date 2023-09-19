using Microsoft.Azure.Devices;
using Shared.Entities.Messages;

namespace Shared.Entities.Factories;


public interface IMessageFactory
{
    T CreateC2DMessageFromMessage<T>(Microsoft.Azure.Devices.Client.Message message) where T : C2DMessages, new();
    Message PrepareC2DMessage(C2DMessages c2dMessage, int expiredMinutes = 60);
}

