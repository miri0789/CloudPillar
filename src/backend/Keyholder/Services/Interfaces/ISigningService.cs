
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task Init();
    Task CreateTwinKeySignature(string deviceId, string keyPath, string signatureKey);
}