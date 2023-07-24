using System.Reflection;
using Microsoft.Azure.Devices;
using Shared.Entities.Messages;

namespace Shared.Entities.Factories;


public class MessagesFactory: IMessagesFactory
{

    public T CreateBaseMessageFromMessage<T>(Microsoft.Azure.Devices.Client.Message message) where T : BaseMessage, new()
    {
        var obj = new T();

        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            object value = message.Properties[property.Name];
            obj.GetType().GetProperty(property.Name).SetValue(obj, value);
        }
        obj.Data = message.GetBytes();
        return obj;
    }

    public Message PrepareBlobMessage(BaseMessage baseMessage, int expiredMinutes = 60)
    {
        var message = new Message(baseMessage.Data)
        {
            MessageId = baseMessage.GetMessageId(),
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };

        PropertyInfo[] properties = baseMessage.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name != "Data")
            {
                message.Properties.Add(property.Name, property.GetValue(baseMessage)?.ToString());
            }
        }
        Console.WriteLine($"BaseMessage PrepareBlobMessage. message title: {baseMessage.MessageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

}

