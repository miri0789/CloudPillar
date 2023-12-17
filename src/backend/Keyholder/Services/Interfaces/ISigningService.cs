
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task CreateTwinKeySignature(string deviceId);
}