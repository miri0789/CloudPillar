namespace Backend.BEApi.Services.Interfaces
{
    public interface IValidateCertificateService
    {
        Task<bool> IsCertificateExpiredAsync(string deviceId);
    }
}