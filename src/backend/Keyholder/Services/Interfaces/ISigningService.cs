
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task<byte[]> GetSigningPublicKeyAsync();
    Task<string> SignData(byte[] data, string deviceId);
}