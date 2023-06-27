using System.Security.Cryptography;
using System.Text;

namespace CloudPillar.Agent.Services;


public interface ISignatureHandler
{
    Task InitPublicKeyAsync();
    bool VerifySignature(string message, string signatureString);
}

public class SignatureHandler : ISignatureHandler
{
    private ECDsa _signingPublicKey;
    public async Task InitPublicKeyAsync()
    {
        string publicKeyPem = await File.ReadAllTextAsync("pki/sign-pubkey.pem");
        _signingPublicKey = LoadPublicKeyFromPem(publicKeyPem);
    }

    private ECDsa LoadPublicKeyFromPem(string pemContent)
    {
        var publicKeyContent = pemContent.Replace("-----BEGIN PUBLIC KEY-----", "")
                                        .Replace("-----END PUBLIC KEY-----", "")
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();

        var publicKeyBytes = Convert.FromBase64String(publicKeyContent);
        var keyReader = new ReadOnlySpan<byte>(publicKeyBytes);

        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(keyReader, out _);

        return ecdsa;
    }

    public bool VerifySignature(string message, string signatureString)
    {
        byte[] signature = Convert.FromBase64String(signatureString);
        byte[] dataToVerify = Encoding.UTF8.GetBytes(message);
        return _signingPublicKey.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA512);
    }
}
