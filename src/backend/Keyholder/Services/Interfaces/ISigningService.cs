
namespace Backend.Keyholder.Interfaces;

public interface ISigningService
{
    Task<byte[]> SignData(byte[] dataToSign);
}