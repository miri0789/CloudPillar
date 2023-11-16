using System.Security.Cryptography.X509Certificates;

public interface IX509Provider
{
    X509Certificate2 GenerateCertificate(string deviceId, string secretKey, int expiredDays);
    X509Certificate2? GetCertificate();
    X509Certificate2 GetHttpsCertificate();
}