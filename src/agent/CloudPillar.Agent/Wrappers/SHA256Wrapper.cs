using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public class SHA256Wrapper : ISHA256Wrapper
{
    public byte[] ComputeHash(byte[] input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(input);
        }
    }
    public int TransformBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset)
    {
        return sha256.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
    }
    public byte[] TransformFinalBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount)
    {
        return sha256.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
    }
    public byte[] GetHash(SHA256 sha256)
    {
        return sha256.Hash;
    }
}