using System.Security.Cryptography;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Shared.Logger;


namespace CloudPillar.Agent.Handlers;


public class SignatureHandler : ISignatureHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private ECDsa _signingPublicKey;
    private readonly ILoggerHandler _logger;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;

    public SignatureHandler(IFileStreamerWrapper fileStreamerWrapper, ILoggerHandler logger, ID2CMessengerHandler d2CMessengerHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
    }
    public async Task InitPublicKeyAsync()
    {
        string publicKeyPem = await _fileStreamerWrapper.ReadAllTextAsync("pki/sign-pubkey.pem");
        if (publicKeyPem == null)
        {
            _logger.Error("sign pubkey not exist");
            throw new ArgumentNullException();
        }
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

    public async Task<bool> VerifySignatureAsync(string message, string signatureString)
    {
        if (_signingPublicKey == null)
        {
            await InitPublicKeyAsync();
        }
        byte[] signature = Convert.FromBase64String(signatureString);
        byte[] dataToVerify = Encoding.UTF8.GetBytes(message);
        return _signingPublicKey.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA512);
    }

    public async Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey)
    {
        await _d2CMessengerHandler.SendSignTwinKeyEventAsync(keyPath, signatureKey);
    }
}
