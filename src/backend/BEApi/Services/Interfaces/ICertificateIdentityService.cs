
namespace Backend.BEApi.Services.Interfaces;

public interface ICertificateIdentityService
{
    Task HandleCertificate(string deviceId);
}