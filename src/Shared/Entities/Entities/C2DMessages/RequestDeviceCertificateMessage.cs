using Microsoft.Azure.Devices.Client;

namespace Shared.Entities.Messages;

public class RequestDeviceCertificateMessage : C2DMessages
{
    public RequestDeviceCertificateMessage()
    {
        MessageType = C2DMessageType.RequestDeviceCertificate;
    }
    public override string GetMessageId()
    {
         return $"RequestDeviceCertificate-{Data}";
    }
}