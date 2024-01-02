using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public interface ISHA256Wrapper
{
    byte[] ComputeHash(byte[] input);
    int TransformBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset);
    byte[] TransformFinalBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount);
    byte[] GetHash(SHA256 sha256);
}