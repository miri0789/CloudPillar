using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public interface IAsymmetricAlgorithmWrapper
{
    bool VerifyData(AsymmetricAlgorithm asymmetricAlgorithm, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm);
}