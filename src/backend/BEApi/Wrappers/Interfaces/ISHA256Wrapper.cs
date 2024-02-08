using System.Security.Cryptography;

namespace Backend.BEApi.Wrappers.Interfaces;
public interface ISHA256Wrapper
{
    SHA256 Create();
    int TransformBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset);
    byte[] TransformFinalBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount);
}