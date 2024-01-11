using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices;

namespace CloudPillar.Agent.Entities;

public class CertificateDetails
{
    public string DeviceId { get; set; }
    public string IotHubHostName { get; set; }
    public string OneMd { get; set; }
}