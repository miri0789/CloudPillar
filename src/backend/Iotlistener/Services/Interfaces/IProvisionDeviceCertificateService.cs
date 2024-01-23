using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IProvisionDeviceService
{
    Task ProvisionDeviceCertificateAsync(string deviceId, ProvisionDeviceCertificateEvent provisionEvent);
    Task RemoveDeviceAsync(string deviceId);
}