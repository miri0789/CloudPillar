using System.Reflection;
using System.Text;
using Microsoft.Azure.Devices;
using Shared.Entities.Messages;

namespace Shared.Entities.Factories;


public class MessageFactory : IMessageFactory
{

    public T CreateC2DMessageFromMessage<T>(Microsoft.Azure.Devices.Client.Message message) where T : C2DMessages, new()
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

    public Message PrepareC2DMessage(C2DMessages c2dMessage, int expiredMinutes = 60)
    {
        var message = new Message(c2dMessage.Data)
        {
            MessageId = c2dMessage.GetMessageId(),
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };

        PropertyInfo[] properties = c2dMessage.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name != "Data")
            {
                message.Properties.Add(property.Name, property.GetValue(c2dMessage)?.ToString());
            }
        }
        Console.WriteLine($"C2DMessages PrepareC2DMessage. message title: {c2dMessage.MessageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

}

