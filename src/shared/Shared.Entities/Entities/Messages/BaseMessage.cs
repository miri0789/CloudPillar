using System.Reflection;
using Microsoft.Azure.Devices;

namespace Shared.Entities.Messages;

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

}

