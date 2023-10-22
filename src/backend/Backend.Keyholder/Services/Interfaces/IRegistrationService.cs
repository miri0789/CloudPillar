using System.Security.Cryptography.X509Certificates;

public interface IRegistrationService
{
    Task Register(string deviceId, string secretKey);

    Task ProvisionDeviceCertificate(string deviceId, byte[] certificate);
}