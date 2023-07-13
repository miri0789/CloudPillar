using System.Reflection;
using Microsoft.Azure.Devices;

namespace shared.Entities.Messages;

public enum MessageType
{
    DownloadChunk
}

public abstract class BaseMessage
{
    public MessageType MessageType { get; set; }
    public Guid ActionGuid { get; set; }
    public byte[] Data { get; set; }
    public abstract string GetMessageId();

    public BaseMessage() { }
    public BaseMessage(Microsoft.Azure.Devices.Client.Message message)
    {
        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            object value = message.Properties[property.Name];
            this.GetType().GetProperty(property.Name).SetValue(this, value);
        }
        this.Data = message.GetBytes();
    }

    public Message PrepareBlobMessage(int expiredMinutes = 60)
    {
        var message = new Message(this.Data)
        {
            MessageId = this.GetMessageId(),
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };

        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name != "Data")
            {
                message.Properties.Add(property.Name, property.GetValue(this)?.ToString());
            }
        }
        Console.WriteLine($"BaseMessage PrepareBlobMessage. message title: {this.MessageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

}

