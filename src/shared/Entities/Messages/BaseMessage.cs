using System.Reflection;
using Microsoft.Azure.Devices;

namespace shared.Entities.Blob;

public enum MessageType
{
    DownloadChunk
}

public abstract class BaseMessage
{
    public abstract MessageType MessageType { get; set; }
    public Guid ActionGuid { get; set; }
    public abstract string GetMessageId();

    public Message PrepareBlobMessage(byte[] data, int expiredMinutes = 60)
    {
        var message = new Message(data)
        {
            MessageId = this.GetMessageId(),
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };

        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            message.Properties.Add(property.Name, property.GetValue(this).ToString());
        }
        Console.WriteLine($"Blobstreamer PrepareBlobMessage. message title: {this.MessageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

    public T CreateObjectFromMessage<T>() where T : BaseMessage, new()
    {
        var obj = new T();

        PropertyInfo[] properties = GetType().GetProperties();
        foreach (var property in properties)
        {
            object value = this.GetType().GetProperty(property.Name).GetValue(this);
            obj.GetType().GetProperty(property.Name).SetValue(obj, value);
        }

        return obj;
    }


}

