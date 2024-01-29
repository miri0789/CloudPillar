using System.Security.Cryptography;
namespace CloudPillar.Agent.Wrappers;
public class AsymmetricAlgorithmWrapper : IAsymmetricAlgorithmWrapper
{
    public bool VerifyData(AsymmetricAlgorithm asymmetricAlgorithm, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
    {
        var keyType = asymmetricAlgorithm!.GetType();
        return keyType.BaseType == typeof(RSA) ? ((RSA)asymmetricAlgorithm).VerifyData(data, signature, hashAlgorithm, RSASignaturePadding.Pkcs1) : ((ECDsa)asymmetricAlgorithm).VerifyData(data, signature, hashAlgorithm);
    }
}