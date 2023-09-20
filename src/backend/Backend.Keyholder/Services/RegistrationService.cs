using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shared.Logger;
using System.Text;
public class RegistrationService : IRegistrationService
{
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string HOST_NAME_EXTENTION_NAME = "iotHubHostName";
    private const int KEY_SIZE_IN_BITS = 4096;
    private readonly ILoggerHandler _loggerHandler;

    public RegistrationService(ILoggerHandler loggerHandler)
    {
        _loggerHandler = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }
    public Task Register(string deviceName, string OneMDKey, string iotHubHostName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(deviceName);
        ArgumentNullException.ThrowIfNullOrEmpty(OneMDKey);
        ArgumentNullException.ThrowIfNullOrEmpty(iotHubHostName);


        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceName}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(OneMDKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(CertificateConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );

            byte[] iotHubHostNameValue = Encoding.UTF8.GetBytes(iotHubHostName);
            var iotHubHostNameExtension = new X509Extension(
                new Oid(CertificateConstants.IOT_HUB_HOST_NAME_EXTENTION_KEY, HOST_NAME_EXTENTION_NAME),
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

            // // Save the certificate to a file
            // System.IO.File.WriteAllText("YourCertificate5.cer", base64Certificate);

            // // Save the certificate to a file
            // System.IO.File.WriteAllBytes("YourCertificate5.pfx", pfxBytes);
        }

    }
}