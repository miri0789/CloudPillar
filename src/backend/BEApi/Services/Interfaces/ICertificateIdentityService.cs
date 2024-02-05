
namespace Backend.BEApi.Services.Interfaces;

public interface ICertificateIdentityService
{
    Task ProcessNewSigningCertificate(string deviceId);
}