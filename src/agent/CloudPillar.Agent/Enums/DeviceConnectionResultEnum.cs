namespace CloudPillar.Agent.Enums;

public enum DeviceConnectionResult
{
    DeviceNotFound = 404001,
    IotHubUnauthorizedAccess = 401002,
    Valid = -1,
    Unknow = -2,
    CertificateInvalid = -3
}