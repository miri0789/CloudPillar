
namespace Shared.Entities.Messages;

public abstract class C2DMessages
{
    public C2DMessageType MessageType { get; set; }
    public int ActionIndex { get; set; }
    public string Error { get; set; }
    public byte[] Data { get; set; }
    public abstract string GetMessageId();

}

