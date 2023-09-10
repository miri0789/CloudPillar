using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shared.Logger;
using System.Text;

namespace CloudPillar.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class GenerateCertificateController : ControllerBase
{
    private readonly ILoggerHandler _logger;

    // this controller is  not for prodaction it will refactoring in task 
    public GenerateCertificateController(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("Generate")]
    public  IActionResult Generate(string deviceName, string OneMDKey)
    {
        if (string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(OneMDKey))
        {
            throw new ArgumentNullException();
        }
        var request = new CertificateRequest(
            $"CN={deviceName}", ECDsa.Create()
            , HashAlgorithmName.SHA256);

        byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(OneMDKey);
        var customExtension = new X509Extension(
            new Oid("1.1.1.1", "OneMDKey"),
            oneMDKeyValue,
            critical: true);
        request.CertificateExtensions.Add(customExtension);

        // Create a self-signed certificate
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1),
            DateTimeOffset.Now.AddDays(30));

        // Export the certificate to a PFX file (password-protected)
        var pfxBytes = certificate.Export(
            X509ContentType.Pkcs12, "1234");

        // Save the certificate to a file
        System.IO.File.WriteAllBytes("YourCertificate2.pfx", pfxBytes);



        return Ok();
    }
}