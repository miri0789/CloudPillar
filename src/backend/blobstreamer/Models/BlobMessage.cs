using System.Reflection;
using Microsoft.Azure.Devices;

namespace blobstreamer.Models;

public class BlobMessage
{
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string Filename { get; set; }

    public Message PrefareBlobMessage(byte[] data)
    {
        int.TryParse(Environment.GetEnvironmentVariable(Constants.messageExpiredMinutes), out int expiredMinutes);
        var message = new Message(data)
        {
            MessageId = $"{this.Filename}_{this.RangeIndex}_{this.ChunkIndex}",
            ExpiryTimeUtc = DateTime.UtcNow.AddMinutes(expiredMinutes)
        };
        
        PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public);
        foreach (var property in properties)
        {
            message.Properties.Add(property.Name.ToLower(), property.GetValue(this).ToString());
        }

        return message;
    }
}

