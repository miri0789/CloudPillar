using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Shared.Entities.Authentication;

namespace CloudPillar.Agent.Utilities;
public static class X509Provider
{
    private const int KEY_SIZE_IN_BITS = 4096;
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    public static X509Certificate2 GenerateCertificate(string deviceId, string secretKey, int expiredDays)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            //  
            SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
            subjectAlternativeNameBuilder.AddDnsName($"{CertificateConstants.CLOUD_PILLAR_SUBJECT}{deviceId}");
            subjectAlternativeNameBuilder.AddDnsName("localhost");
            request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());

            byte[] oneMDKeyValue = Encoding.UTF8.GetBytes(secretKey);
            var OneMDKeyExtension = new X509Extension(
                new Oid(ProvisioningConstants.ONE_MD_EXTENTION_KEY, ONE_MD_EXTENTION_NAME),
                oneMDKeyValue, false
               );


            request.CertificateExtensions.Add(OneMDKeyExtension);



            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(expiredDays));

            return certificate;

        }
    }

    public static X509Certificate2? GetCertificate()
    {
        var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        using (store)
        {
            var certificates = store.Certificates;
            var filteredCertificate = certificates?.Cast<X509Certificate2>()
              .Where(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public static X509Certificate2 GetHttpsCertificate()
    {
        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates;
            var filteredCertificate = certificates?.Cast<X509Certificate2>()
              .Where(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT))
               .FirstOrDefault();

            if (filteredCertificate == null)
            {
                var temporaryAnonymousCertificate = certificates?.Cast<X509Certificate2>()
                            .Where(cert => cert.Subject == ProvisioningConstants.CERTIFICATE_SUBJECT + "CP-Temporary-anonymous")
                            .FirstOrDefault();
                if (temporaryAnonymousCertificate != null)
                {
                    return temporaryAnonymousCertificate;
                }
            }
            else return filteredCertificate;
        }
        return GenerateTemporaryAnonymousCertificate();
    }

    private static X509Certificate2 GenerateTemporaryAnonymousCertificate()
    {
        X509Certificate2 certificate;
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}CP-Temporary-anonymous", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
            subjectAlternativeNameBuilder.AddDnsName("localhost");
            request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());
            certificate = request.CreateSelfSigned(
               DateTimeOffset.Now.AddDays(-1),
               DateTimeOffset.Now.AddDays(365));
        }
        var password = new Guid().ToString();
        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, password);
        var privateCertificate = new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.PersistKeySet);
        using (var store = new X509Store())
        {
            store.Certificates.Add(privateCertificate);
        }
        return privateCertificate;
    }
}