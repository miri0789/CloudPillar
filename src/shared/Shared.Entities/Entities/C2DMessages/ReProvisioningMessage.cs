
using System.Text;

namespace Shared.Entities.Messages;

public class ReprovisioningMessage : C2DMessages
{
    public string ScopedId { get; set; }

    public string DeviceEndpoint { get; set; }

    public string DPSConnectionString { get; set; }

    public ReprovisioningMessage()
    {
        MessageType = C2DMessageType.Reprovisioning;
    }

    public override string GetMessageId()
    {
        return $"sendCertificate_{Encoding.ASCII.GetString(Data)}";
    }

}