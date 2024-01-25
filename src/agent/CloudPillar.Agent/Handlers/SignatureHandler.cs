using System.Security.Cryptography;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class SignatureHandler : ISignatureHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ILoggerHandler _logger;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ISHA256Wrapper _sha256Wrapper;
    private readonly IAsymmetricAlgorithmWrapper _ecdsaWrapper;
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private readonly DownloadSettings _downloadSettings;
    private const string FILE_EXTENSION = "*.cer";

    public SignatureHandler(IFileStreamerWrapper fileStreamerWrapper, ILoggerHandler logger, ID2CMessengerHandler d2CMessengerHandler,
    ISHA256Wrapper sha256Wrapper, IAsymmetricAlgorithmWrapper ecdsaWrapper, IServerIdentityHandler serverIdentityHandler, IOptions<DownloadSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _sha256Wrapper = sha256Wrapper ?? throw new ArgumentNullException(nameof(sha256Wrapper));
        _ecdsaWrapper = ecdsaWrapper ?? throw new ArgumentNullException(nameof(ecdsaWrapper));
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
        _downloadSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    private async Task<AsymmetricAlgorithm> InitPublicKeyAsync(string publicKeyPem)
    {
        if (publicKeyPem == null)
        {
            _logger.Error("sign pubkey not exist");
            throw new ArgumentNullException();
        }
        return LoadPublicKeyFromPem(publicKeyPem);
    }

    private AsymmetricAlgorithm LoadPublicKeyFromPem(string pemContent)
    {
        bool isRSA = pemContent.Contains("RSA PUBLIC KEY");
        var publicKeyContent = pemContent.Replace("-----BEGIN PUBLIC KEY-----", "")
                                        .Replace("-----END PUBLIC KEY-----", "")
                                        .Replace("-----BEGIN RSA PUBLIC KEY-----", "")
                                        .Replace("-----END RSA PUBLIC KEY-----", "")
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();

        var publicKeyBytes = Convert.FromBase64String(publicKeyContent);
        var keyReader = new ReadOnlySpan<byte>(publicKeyBytes);

        if(isRSA)
        {
            RSA rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyReader, out _);
            _logger.Debug($"Imported public key");
            return rsa;
        }
        ECDsa ecdsa  = ECDsa.Create();
        ecdsa .ImportSubjectPublicKeyInfo(keyReader, out _);
        _logger.Debug($"Imported public key");
        return ecdsa ;
    }

    public async Task<bool> VerifySignatureAsync(byte[] dataToVerify, string signatureString)
    {
        string[] publicKeyFiles = _fileStreamerWrapper.GetFiles(Constants.PKI_FOLDER_PATH, FILE_EXTENSION);
        foreach (string publicKeyFile in publicKeyFiles)
        {
            var publicKey = await _serverIdentityHandler.GetPublicKeyFromCertificateFileAsync(publicKeyFile);
            using (var signingPublicKey = await InitPublicKeyAsync(publicKey))
            {
                byte[] signature = Convert.FromBase64String(signatureString);
                if (_ecdsaWrapper.VerifyData(signingPublicKey, dataToVerify, signature, HashAlgorithmName.SHA512))
                {
                    return true;
                }
            }
        }
        return false;
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
            _logger.Error($"VerifyFileSignatureAsync failed message: {ex.Message}");
            return false;
        }
    }

    private byte[] CalculateHash(string filePath)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            using (FileStream fileStream = _fileStreamerWrapper.OpenRead(filePath))
            {
                byte[] buffer = new byte[_downloadSettings.SignFileBufferSize];
                int bytesRead;

                while ((bytesRead = _fileStreamerWrapper.Read(fileStream, buffer, 0, buffer.Length)) > 0)
                {
                    _sha256Wrapper.TransformBlock(sha256, buffer, 0, bytesRead, null, 0);
                }

                _sha256Wrapper.TransformFinalBlock(sha256, new byte[0], 0, 0);
                return _sha256Wrapper.GetHash(sha256);
            }
        }
    }

    public async Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken)
    {
        await _d2CMessengerHandler.SendSignTwinKeyEventAsync(keyPath, signatureKey, cancellationToken);
    }
}
