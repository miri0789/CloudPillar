using System.Security.Cryptography;
namespace CloudPillar.Agent.Wrappers;
public class AsymmetricAlgorithmWrapper : IAsymmetricAlgorithmWrapper
{
    public bool VerifyData(AsymmetricAlgorithm asymmetricAlgorithm, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
    {
        var keyType = asymmetricAlgorithm!.GetType();
        return keyType.BaseType == typeof(RSA) ? ((RSA)asymmetricAlgorithm).VerifyData(data, signature, hashAlgorithm, RSASignaturePadding.Pkcs1) : ((ECDsa)asymmetricAlgorithm).VerifyData(data, signature, hashAlgorithm);
    }


    public RSA GetRSAKey(byte[] bytes)
    {
        var keyReader = new ReadOnlySpan<byte>(bytes);

        RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(keyReader, out _);
        return rsa;
    }

    public ECDsa GetECDsaKey(byte[] bytes)
    {
        var keyReader = new ReadOnlySpan<byte>(bytes);

        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(keyReader, out _);
        return ecdsa;
    }
}