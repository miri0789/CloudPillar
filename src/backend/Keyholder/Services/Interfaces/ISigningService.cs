
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task<byte[]> SignData(byte[] dataToSign);
    Task CreateFileKeySignature(string deviceId, string propName, int actionIndex, byte[] hash, string changeSpecKey);
}