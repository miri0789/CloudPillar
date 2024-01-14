using System.Security.Cryptography.X509Certificates;
using Backend.BEApi.Services.Interfaces;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class CertificateIdentityService : ICertificateIdentityService
{
    private readonly ILoggerHandler _logger;

    public CertificateIdentityService(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public void ExportCerFromPfxFile(X509Certificate2 certificate)
    {
        try
        {
        
            certificate = new X509Certificate2("C:\\git.dev\\CloudPillar\\CloudPillar\\src\\agent\\CloudPillar.Agent\\pki\\pk-certificate.pfx");
            // Save the public key to a CER file
            byte[] certData = certificate.Export(X509ContentType.Cert);

            X509Certificate2 myCert = new X509Certificate2();
            // Import from the byte array
            myCert.Import(certData);


            X509Certificate2 oCert = new X509Certificate2("certificate.pfx", "123456");

            Byte[] aCert = oCert.Export(X509ContentType.Cert);
            File.WriteAllBytes("certificato.cer", aCert);


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void UploadCertificateToBlob(X509Certificate2 certificate)
    {
        try
        {
            // Save the public key to a CER file
            byte[] certData = certificate.Export(X509ContentType.Cert);

            X509Certificate2 myCert = new X509Certificate2();
            // Import from the byte array
            myCert.Import(certData);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}