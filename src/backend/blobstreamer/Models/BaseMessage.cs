using System.Reflection;
using Microsoft.Azure.Devices;

namespace blobstreamer.Models;

public enum MessageType
{
    Start,
    Chunk,
    End
}

public abstract class BaseMessage
{
    public abstract MessageType messageType { get; set; }
    public abstract string GetMessageId();

    public Message PrepareBlobMessage(byte[] data)
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
        Console.WriteLine($"Blobstreamer PrepareBlobMessage. message title: {this.messageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

}

