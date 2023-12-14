using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class ProvisionDeviceCertificateEvent : D2CMessage
{
    public byte[] Data { get; set; }
    public string CertificatePrefix { get; set; }
    public ProvisionDeviceCertificateEvent()
    {
        this.MessageType = D2CMessageType.ProvisionDeviceCertificate;
    }
}