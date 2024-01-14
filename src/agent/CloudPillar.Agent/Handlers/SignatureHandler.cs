using System.Security.Cryptography;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Entities;
using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;

public class SignatureHandler : ISignatureHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ILoggerHandler _logger;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ISHA256Wrapper _sha256Wrapper;
    private readonly IECDsaWrapper _ecdsaWrapper;
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private readonly IX509CertificateWrapper _x509CertificateWrapper;
    private readonly DownloadSettings _downloadSettings;
    private const string FILE_EXTENSION = "*.cer";

    public SignatureHandler(IFileStreamerWrapper fileStreamerWrapper, ILoggerHandler logger, ID2CMessengerHandler d2CMessengerHandler,
    ISHA256Wrapper sha256Wrapper, IECDsaWrapper ecdsaWrapper, IServerIdentityHandler serverIdentityHandler, IX509CertificateWrapper x509CertificateWrapper, IOptions<DownloadSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _sha256Wrapper = sha256Wrapper ?? throw new ArgumentNullException(nameof(sha256Wrapper));
        _ecdsaWrapper = ecdsaWrapper ?? throw new ArgumentNullException(nameof(ecdsaWrapper));
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
        _downloadSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _x509CertificateWrapper = x509CertificateWrapper ?? throw new ArgumentNullException(nameof(x509CertificateWrapper));
    }

    private async Task<ECDsa> InitPublicKeyAsync(string publicKeyPem)
    {
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

        ECDsa ecdsa = _ecdsaWrapper.Create();
        _ecdsaWrapper.ImportSubjectPublicKeyInfo(ecdsa, keyReader);
        return ecdsa;
    }

    public async Task<bool> VerifySignatureAsync(byte[] dataToVerify, string signatureString)
    {
        string[] publicKeyFiles = _fileStreamerWrapper.GetFiles(Constants.PKI_FOLDER_PATH, FILE_EXTENSION);
        foreach (string publicKeyFile in publicKeyFiles)
        {
            X509Certificate2 certificate = _x509CertificateWrapper.CreateFromFile(publicKeyFile);
            if (certificate.NotAfter < DateTime.Now)
            {
                throw new Exception($"GetPublicKeyFromCertificateFileAsync certificate {publicKeyFile} is expired");
            }

            var publicKey = await _serverIdentityHandler.GetPublicKeyFromCertificate(certificate);
            using (var ecdsa = await InitPublicKeyAsync(publicKey))
            {
                byte[] signature = Convert.FromBase64String(signatureString);
                if (_ecdsaWrapper.VerifyData(ecdsa, dataToVerify, signature, HashAlgorithmName.SHA512))
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
