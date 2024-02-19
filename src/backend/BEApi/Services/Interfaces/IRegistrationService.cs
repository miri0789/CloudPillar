namespace Backend.BEApi.Services.Interfaces;

public interface IRegistrationService
{
    Task RegisterAsync(string deviceId, string secretKey);
    
    Task RegisterByOneMDReportAsync(string deviceId, string secretKey);

    Task ProvisionDeviceCertificateAsync(string deviceId, string prefix, byte[] certificate);
}