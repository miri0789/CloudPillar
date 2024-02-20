
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Enums;
using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Handlers;
public interface IDPSProvisioningDeviceClientHandler
{
    Task<DeviceConnectionResult> ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint, Message message, CancellationToken cancellationToken);

    X509Certificate2? GetCertificate(string deviceId = "");

    Task<DeviceConnectionResult> AuthorizationDeviceAsync(string XdeviceId, string XSecretKey, CancellationToken cancellationToken, bool checkAuthorization = false);

    Task<DeviceConnectionResult> InitAuthorizationAsync();
}