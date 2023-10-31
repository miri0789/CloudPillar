using System.Security.Cryptography.X509Certificates;
using Shared.Entities.Authentication;

public static class X509Helper
{
    public static X509Certificate2? GetCertificate()
    {
        var store = new X509Store(StoreName.Root,StoreLocation.CurrentUser);
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

}