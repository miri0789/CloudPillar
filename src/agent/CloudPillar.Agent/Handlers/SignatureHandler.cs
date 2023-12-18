using System.Security.Cryptography;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Handlers;

public class SignatureHandler : ISignatureHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ILoggerHandler _logger;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly SignFileSettings _signFileSettings;

    public SignatureHandler(IFileStreamerWrapper fileStreamerWrapper, ILoggerHandler logger, ID2CMessengerHandler d2CMessengerHandler, IOptions<SignFileSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _signFileSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }
    private async Task<ECDsa> InitPublicKeyAsync()
    {
        string publicKeyPem = await _fileStreamerWrapper.ReadAllTextAsync("pki/sign-pubkey.pem");
        if (publicKeyPem == null)
        {
            _logger.Error("sign pubkey not exist");
            throw new ArgumentNullException();
        }
        return (ECDsa)LoadPublicKeyFromPem(publicKeyPem);
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

    public async Task<bool> VerifySignatureAsync(byte[] dataToVerify, string signatureString)
    {
        using (var ecdsa = await InitPublicKeyAsync())
        {
            byte[] signature = Convert.FromBase64String(signatureString);
            return ecdsa.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA512);
        }
    }

    public async Task<bool> VerifyFileSignatureAsync(string filePath, string signature)
    {
        try
        {
            byte[] hash = CalculateHash(filePath);
            return await VerifySignatureAsync(hash, signature);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            return false;
        }
    }

    private byte[] CalculateHash(string filePath)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[_signFileSettings.BufferSize];
                int bytesRead;

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(new byte[0], 0, 0);
                return sha256.Hash;
            }
        }
    }

    public async Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken)
    {
        await _d2CMessengerHandler.SendSignTwinKeyEventAsync(keyPath, signatureKey, cancellationToken);
    }
}
