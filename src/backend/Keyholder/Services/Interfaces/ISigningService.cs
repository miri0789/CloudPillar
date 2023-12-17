
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task Init();
    Task CreateTwinKeySignature(string deviceId);
    Task CreateFileKeySignature(string deviceId, string propName, int actionIndex, byte[] hash);
}