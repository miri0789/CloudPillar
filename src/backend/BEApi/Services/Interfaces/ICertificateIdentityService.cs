using System.Security.Cryptography.X509Certificates;

namespace Backend.BEApi.Services.Interfaces;

public interface ICertificateIdentityService
{
    void ExportCerFromPfxFile(X509Certificate2 certificate);
    Task UploadCertificateToBlob(X509Certificate2 certificate);
}