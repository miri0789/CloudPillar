
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task CreateTwinKeySignature(string deviceId, string changeSignKey);
    Task CreateFileKeySignature(string deviceId, string propName, int actionIndex, byte[] hash, string changeSpecKey);
    Task<string> GetSigningPublicKeyAsync();
}