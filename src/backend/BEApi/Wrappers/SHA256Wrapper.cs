using System.Security.Cryptography;
using Backend.BEApi.Wrappers.Interfaces;

namespace CloudPillar.Agent.Wrappers;
public class SHA256Wrapper : ISHA256Wrapper
{
    public SHA256 Create()
    {
        return SHA256.Create();
    }

    public int TransformBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset)
    {
        return sha256.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
    }
    
    public byte[] TransformFinalBlock(SHA256 sha256, byte[] inputBuffer, int inputOffset, int inputCount)
    {
        return sha256.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
    }
}