using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Utilities;


public class X509Provider : IX509Provider
{
    private const int KEY_SIZE_IN_BITS = 4096;
    private const string ONE_MD_EXTENTION_NAME = "OneMDKey";
    private const string DNS_NAME = "localhost";
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly AuthenticationSettings _authenticationSettings;

    public X509Provider(IX509CertificateWrapper X509CertificateWrapper, IOptions<AuthenticationSettings> options)
    {
        _x509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _authenticationSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public X509Certificate2 GenerateCertificate(string deviceId, string secretKey, int expiredDays)
    {
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{_authenticationSettings.GetCertificatePrefix()}{deviceId}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
            subjectAlternativeNameBuilder.AddDnsName($"{_authenticationSettings.GetCertificatePrefix()}{deviceId}");
            subjectAlternativeNameBuilder.AddDnsName(DNS_NAME);
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


    public X509Certificate2 GetHttpsCertificate()
    {
        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadOnly, StoreName.My))
        {
            var certificates = _x509CertificateWrapper.GetCertificates(store);
            var filteredCertificate = GetCPCertificate(certificates);

            if (filteredCertificate == null)
            {
                var temporaryAnonymousCertificate = certificates?.Cast<X509Certificate2>()
                            .FirstOrDefault(cert => cert.Subject == ProvisioningConstants.CERTIFICATE_SUBJECT + _authenticationSettings.GetAnonymousCertificate());

                if (temporaryAnonymousCertificate != null)
                {
                    return temporaryAnonymousCertificate;
                }
            }
            else return filteredCertificate;
        }
        return GenerateTemporaryAnonymousCertificate();
    }

    private X509Certificate2? GetCPCertificate(X509Certificate2Collection certificates)
    {
        return certificates?.Cast<X509Certificate2>()
                      .FirstOrDefault(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + _authenticationSettings.GetCertificatePrefix()));
    }

    private X509Certificate2 GenerateTemporaryAnonymousCertificate()
    {
        X509Certificate2 certificate;
        using (RSA rsa = RSA.Create(KEY_SIZE_IN_BITS))
        {
            var request = new CertificateRequest(
                $"{ProvisioningConstants.CERTIFICATE_SUBJECT}{_authenticationSettings.GetAnonymousCertificate()}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
            subjectAlternativeNameBuilder.AddDnsName(DNS_NAME);
            request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());
            certificate = request.CreateSelfSigned(
               DateTimeOffset.Now.AddDays(-1),
               DateTimeOffset.Now.AddDays(_authenticationSettings.CertificateExpiredDays));
        }
        var password = Guid.NewGuid().ToString();
        var pfxBytes = certificate.Export(X509ContentType.Pkcs12, password);
        var privateCertificate = new X509Certificate2(pfxBytes, password)
        {
            FriendlyName = "cloud pillar agent anonymous"
        };
        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadWrite, StoreName.My))
        {
            store.Add(privateCertificate);
        }
        return privateCertificate;
    }
}