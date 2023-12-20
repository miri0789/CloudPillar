public interface IRegistrationService
{
    Task RegisterAsync(string deviceId, string secretKey);

    Task ProvisionDeviceCertificateAsync(string deviceId, string prefix, byte[] certificate);
}