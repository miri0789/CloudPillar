
using System.Reflection;
using Microsoft.Azure.Devices;

namespace blobstreamer.Models;

public class BlobMessage
{
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string Filename { get; set; }
    public int RangeSize { get; set; }

    public string GetMessageId()
    {
        return $"{this.Filename}_{this.RangeIndex}_{this.ChunkIndex}";
    }

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
        Console.WriteLine($"Blobstreamer PrepareBlobMessage. properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }
}

