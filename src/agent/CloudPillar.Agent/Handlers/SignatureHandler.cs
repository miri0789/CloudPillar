using System.Security.Cryptography;
using System.Text;
using CloudPillar.Agent.Wrappers;


namespace CloudPillar.Agent.Handlers;


public class SignatureHandler : ISignatureHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private ECDsa _signingPublicKey;

    public SignatureHandler(IFileStreamerWrapper fileStreamerWrapper)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
    }
    public async Task InitPublicKeyAsync()
    {
        string publicKeyPem = await _fileStreamerWrapper.ReadAllTextAsync("pki/sign-pubkey.pem");
        _signingPublicKey = (ECDsa)LoadPublicKeyFromPem(publicKeyPem);
    }

    private AsymmetricAlgorithm LoadPublicKeyFromPem(string pemContent)
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
