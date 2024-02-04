
namespace Backend.BEApi.Services.Interfaces;

public interface ICertificateIdentityService
{
    Task ProcessUpdatingAgentInNewCertificate(string deviceId);
}