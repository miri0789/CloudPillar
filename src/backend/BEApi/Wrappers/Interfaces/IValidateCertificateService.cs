namespace Backend.BEApi.Services.Interfaces;

public interface IValidateCertificateService
{
    Task IsDevicesCertificateExpiredAsync();
}
