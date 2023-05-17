using System.Reflection;
using Microsoft.Azure.Devices;

namespace blobstreamer.Models;

public abstract class BaseMessage
{
    public abstract string GetMessageId();

    public Message PrepareBlobMessage(byte[] data, BaseMessage messageType)
    {
        int.TryParse(Environment.GetEnvironmentVariable(Constants.messageExpiredMinutes), out int expiredMinutes);
        var message = new Message(data)
        {
            MessageId = this.GetMessageId(),
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };

        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            message.Properties.Add(property.Name.ToLower(), property.GetValue(this).ToString());
        }

        return message;
    }

}

