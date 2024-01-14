using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public interface IECDsaWrapper
{
    ECDsa Create();
    bool VerifyData(ECDsa ecdsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm);
    void ImportSubjectPublicKeyInfo(ECDsa ecdsa, ReadOnlySpan<byte> keyReader);
}