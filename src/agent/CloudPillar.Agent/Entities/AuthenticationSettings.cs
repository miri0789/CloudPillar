using System.Runtime.ConstrainedExecution;
using Shared.Entities.Authentication;

public class AuthenticationSettings
{
    private const int DEFUALT_EXPIRED_DAYS = 365;
    private const string GLOBAL_DEVICE_ENDPOINT = "global.azure-devices-provisioning.net";

    public string? DpsScopeId { get; set; }
    public string GlobalDeviceEndpoint { get; set; } = GLOBAL_DEVICE_ENDPOINT;
    public int CertificateExpiredDays { get; set; } = DEFUALT_EXPIRED_DAYS;
    public string? GroupEnrollmentKey { get; set; }
    public string Environment { get; set; }
    public string CertificatePrefix { get; set; } = CertificateConstants.CLOUD_PILLAR_SUBJECT;
    public string GetCertificatePrefix()
    {
        var prefix = CertificatePrefix ?? CertificateConstants.CLOUD_PILLAR_SUBJECT;
        return !string.IsNullOrEmpty(Environment) ? $"{prefix}-{Environment}-" : $"{prefix}-";
    }





}