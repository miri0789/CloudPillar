namespace Shared.Entities.Messages;
public class RemoveDeviceEvent : D2CMessage
{
    public RemoveDeviceEvent()
    {
        this.MessageType = D2CMessageType.RemoveDevice;
    }
}