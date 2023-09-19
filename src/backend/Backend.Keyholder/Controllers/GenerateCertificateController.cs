using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shared.Logger;
using System.Text;

namespace Backend.Keyholder;

[ApiController]
[Route("[controller]")]
public class GenerateCertificateController : ControllerBase
{
    private readonly ILoggerHandler _logger;

    // this controller is  not for prodaction it will be refactoring in other task 
    public GenerateCertificateController(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("Generate")]

    public IActionResult Generate(string deviceName, string OneMDKey, string iotHubHostName)
    {
        if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(OneMDKey))
        {
            throw new ArgumentNullException();
        }

        using (RSA rsa = RSA.Create(4096))
        {


            var request = new CertificateRequest(
                $"CN=CloudPillar-{deviceName}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(OneMDKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid("1.1.1.1", "OneMDKey"),
                oneMDKeyValue, false
               );

            byte[] iotHubHostNameValue = Encoding.UTF8.GetBytes(iotHubHostName);
            var iotHubHostNameExtension = new X509Extension(
                new Oid("2.2.2.2", "iotHubHostName"),
                iotHubHostNameValue, false
               );

            request.CertificateExtensions.Add(OneMDKeyExtension);
            request.CertificateExtensions.Add(iotHubHostNameExtension);

            // Create a self-signed certificate
            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(30));



            // Export the certificate to a PFX file (password-protected)
            var pfxBytes = certificate.Export(
                X509ContentType.Pkcs12, "1234");

            string base64Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

            // Save the certificate to a file
            System.IO.File.WriteAllText("YourCertificate5.cer", base64Certificate);

            // Save the certificate to a file
            System.IO.File.WriteAllBytes("YourCertificate5.pfx", pfxBytes);
        }



        return Ok();
    }
}