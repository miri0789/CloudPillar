using System.Security.Cryptography;
using CloudPillar.Agent.Handlers.Logger;
namespace CloudPillar.Agent.Wrappers;
public class ECDsaWrapper : IECDsaWrapper
{
    public bool VerifyData(ECDsa ecdsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
    {
        return ecdsa.VerifyData(data, signature, hashAlgorithm);
    }
}