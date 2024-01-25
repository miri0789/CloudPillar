namespace Backend.BEApi.Services.Interfaces;

public interface IDeviceCertificateService
{
    Task IsDevicesCertificateExpiredAsync();
    Task RemoveDeviceAsync(string deviceId);
}
