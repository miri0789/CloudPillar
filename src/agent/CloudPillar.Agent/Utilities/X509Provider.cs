using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Shared.Entities.Authentication;

namespace CloudPillar.Agent.Utilities;
public static class X509Provider
{
    private const int KEY_SIZE_IN_BITS = 4096;
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string LOCALHOST_DNS_NAME = "localhost";
    public static X509Certificate2 GenerateCertificate(string deviceId, string secretKey, int expiredDays)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(secretKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(ProvisioningConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );

            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(LOCALHOST_DNS_NAME);

            request.CertificateExtensions.Add(sanBuilder.Build());

            request.CertificateExtensions.Add(OneMDKeyExtension);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(expiredDays));

            return certificate;

        }
    }

}