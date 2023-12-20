using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IProvisionDeviceCertificateService
{
    Task ProvisionDeviceCertificateAsync(string deviceId, ProvisionDeviceCertificateEvent provisionEvent);
}