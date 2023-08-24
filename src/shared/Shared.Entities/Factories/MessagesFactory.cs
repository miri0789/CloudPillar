using System.Reflection;
using System.Text;
using Microsoft.Azure.Devices;
using Shared.Entities.Messages;

namespace Shared.Entities.Factories;


public class MessagesFactory : IMessagesFactory
{

    public T CreateBaseMessageFromMessage<T>(Microsoft.Azure.Devices.Client.Message message) where T : BaseMessage, new()
    {
        var obj = new T();

        PropertyInfo[] properties = obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (message.Properties.TryGetValue(property.Name, out string valueStr))
            {
                Type propertyType = property.PropertyType;

                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }
                object value = propertyType.IsEnum ? Enum.Parse(propertyType, valueStr) : Convert.ChangeType(valueStr, propertyType);
                obj.GetType().GetProperty(property.Name).SetValue(obj, value);
            }
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

