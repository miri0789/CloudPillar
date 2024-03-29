using System.Security.Cryptography.X509Certificates;

public class AuthenticationSettings
{
    private const int DEFUALT_EXPIRED_DAYS = 365 * 2;
    private const string GLOBAL_DEVICE_ENDPOINT = "global.azure-devices-provisioning.net";
    private const string ANONYMOUS_CERTIFICATE = "Temporary-anonymous";
    public string TransportType { get; set; }

    public string? DpsScopeId { get; set; }
    public string GlobalDeviceEndpoint { get; set; } = GLOBAL_DEVICE_ENDPOINT;
    public int CertificateExpiredDays { get; set; } = DEFUALT_EXPIRED_DAYS;
    public string? GroupEnrollmentKey { get; set; }
    public string Environment { get; set; }
    public string CertificatePrefix { get; set; } = ProvisioningConstants.CLOUD_PILLAR_SUBJECT;
    public string? Domain { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public StoreLocation StoreLocation { get; set; }
    public string[] ValidDomains { get; set; } = new string[] {  };

    public string GetCertificatePrefix()
    {
        var prefix = CertificatePrefix ?? ProvisioningConstants.CLOUD_PILLAR_SUBJECT;
        return !string.IsNullOrEmpty(Environment) ? $"{prefix}-{Environment}-" : $"{prefix}-";
    }
    public string GetAnonymousCertificate()
    {
        var tempPrefixCertificate = $"{GetCertificatePrefix()}{ANONYMOUS_CERTIFICATE}";
        return tempPrefixCertificate;
    }

    public string GetTemporaryCertificate()
    {
        var tempPrefixCertificate = $"{GetCertificatePrefix()}{ProvisioningConstants.TEMPORARY_CERTIFICATE_NAME}";
        return tempPrefixCertificate;
    }
}