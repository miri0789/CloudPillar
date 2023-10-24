using System.Security.Cryptography.X509Certificates;

public interface IRegistrationService
{
    Task RegisterAsync(string deviceId, string secretKey);

    Task ProvisionDeviceCertificateAsync(string deviceId, byte[] certificate);
}