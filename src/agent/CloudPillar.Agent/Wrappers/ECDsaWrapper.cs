using System.Security.Cryptography;
namespace CloudPillar.Agent.Wrappers;

public class ECDsaWrapper : IECDsaWrapper
{
    public ECDsa Create()
    {
        return ECDsa.Create();
    }
    public bool VerifyData(ECDsa ecdsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
    {
        return ecdsa.VerifyData(data, signature, hashAlgorithm);
    }

    public void ImportSubjectPublicKeyInfo(ECDsa ecdsa, ReadOnlySpan<byte> keyReader)
    {
        ecdsa.ImportSubjectPublicKeyInfo(keyReader, out _);
    }
}