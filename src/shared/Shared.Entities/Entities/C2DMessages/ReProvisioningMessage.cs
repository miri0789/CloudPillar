
namespace Shared.Entities.Messages;

public class ReProvisioningMessage : C2DMessages
{
    public string EnrollmentId { get; set; }

    public string ScopedId { get; set; }

    public string DeviceEndpoint { get; set; }

    public string PasswordFunc { get; set; }

    public ReProvisioningMessage()
    {
        MessageType = C2DMessageType.ReProvisioning;
    }

    public override string GetMessageId()
    {
        return $"sendCertificate_{this.EnrollmentId}";
    }

}