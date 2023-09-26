
namespace Shared.Entities.Messages;

public class RegisterCertificateMessage : C2DMessages
{
    public string  Certificate { get; set; }

    public string Password { get; set; }

    public RegisterCertificateMessage()
    {
        MessageType = C2DMessageType.RegisterCertificate;
    }

    public override string GetMessageId()
    {
        return "sendCertificate";
    }

}