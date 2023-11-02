public class AuthonticationSettings
{
    private const int DEFUALT_EXPIRED_DAYS = 365;
    private const string GLOBAL_DEVICE_ENDPOINT = "global.azure-devices-provisioning.net";

    public string DpsScopeId { get; set; }
    public string GlobalDeviceEndpoint { get; set; } = GLOBAL_DEVICE_ENDPOINT;
    public int CertificateExpiredDays { get; set; } = DEFUALT_EXPIRED_DAYS;
    public string GroupEnrollmentKey { get; set; }
   
}