using shared.Entities.Events;

namespace iotlistener.Interfaces;

public interface ISigningService
{
    Task CreateTwinKeySignature(string deviceId, SignEvent signEvent);
}