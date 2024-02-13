namespace CloudPillar.Agent.Entities;

public enum DeviceConnectResultEnum
{
    DeviceNotFound = 404001,
    IotHubUnauthorizedAccess = 401002,
    Valid = -1,
    Unknow = -2,
    CertificateInvalid = -3
}