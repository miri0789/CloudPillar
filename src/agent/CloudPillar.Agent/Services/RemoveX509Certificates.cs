using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Sevices.interfaces;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Sevices;

public class RemoveX509Certificates : IRemoveX509Certificates
{
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly AuthenticationSettings _authenticationSettings;

    public RemoveX509Certificates(IX509CertificateWrapper x509CertificateWrapper, IOptions<AuthenticationSettings> options)
    {
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
        _authenticationSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public void RemoveX509CertificatesFromStore()
    {
        using (var store = _x509CertificateWrapper.Open(OpenFlags.ReadWrite, storeLocation: _authenticationSettings.StoreLocation))
        {
            RemoveCertificatesFromStore(store, string.Empty);
        }
    }

    public void RemoveCertificatesFromStore(X509Store store, string thumbprint)
    {
        var certificates = _x509CertificateWrapper.GetCertificates(store);

        var filteredCertificates = certificates?.Cast<X509Certificate2>()
           .Where(cert => string.IsNullOrEmpty(thumbprint) && cert.FriendlyName == _authenticationSettings.GetTemporaryCertificate() ||
            (cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + _authenticationSettings.GetCertificatePrefix())
           && cert.Thumbprint != thumbprint))
           .ToArray();

        if (filteredCertificates?.Length > 0)
        {
            var certificateCollection = _x509CertificateWrapper.CreateCertificateCollecation(filteredCertificates);
            _x509CertificateWrapper.RemoveRange(store, certificateCollection);
        }
    }
}