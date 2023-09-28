using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface ISigningService
{
    Task CreateTwinKeySignature(string deviceId, SignEvent signEvent);
}