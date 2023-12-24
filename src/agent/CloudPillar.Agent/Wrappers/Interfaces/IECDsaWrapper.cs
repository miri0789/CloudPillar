using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public interface IECDsaWrapper
{
    bool VerifyData(ECDsa ecdsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm);
}