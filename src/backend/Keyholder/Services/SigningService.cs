using System.Security.Cryptography;
using System.Text;
using Backend.Keyholder.Interfaces;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using Backend.Infra.Common.Wrappers.Interfaces;
using System.Security.Cryptography.X509Certificates;

namespace Backend.Keyholder.Services;

public class SigningService : ISigningService
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;

    private const string PRIVATE_KEY_FILE = "tls.key";
    private const string PUBLIC_KEY_FILE = "tls.crt";
    private const string BEGIN_CERTIFICATE = "-----BEGIN CERTIFICATE-----";

    public SigningService(IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger, IRegistryManagerWrapper registryManagerWrapper, IFileStreamerWrapper fileStreamerWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));

        if (string.IsNullOrEmpty(_environmentsWrapper.iothubConnectionString))
        {
            throw new ArgumentNullException(nameof(_environmentsWrapper.iothubConnectionString));
        }
    }

    public async Task<byte[]> GetSigningPublicKeyAsync()
    {
        string secretVolumeMountPath = _environmentsWrapper.SecretVolumeMountPath;

        if (string.IsNullOrWhiteSpace(secretVolumeMountPath))
        {
            var message = "default cert secret path must be set.";
            _logger.Error(message);
            throw new InvalidOperationException(message);
        }

        var publicKeyPem = await ReadKeyFromMount(Path.Combine(secretVolumeMountPath, PUBLIC_KEY_FILE));
        if (string.IsNullOrWhiteSpace(publicKeyPem))
        {
            var message = "public key is empty.";
            _logger.Error(message);
            throw new InvalidOperationException(message);
        }
        var sectionsCount = publicKeyPem.Split(BEGIN_CERTIFICATE).Count();
        if (sectionsCount > 1)
        {
            publicKeyPem = GetLastCertificateSection(publicKeyPem);
        }

        return Encoding.UTF8.GetBytes(publicKeyPem);
    }

    private async Task<AsymmetricAlgorithm> GetSigningPrivateKeyAsync(string deviceId)
    {
        _logger.Info("In kube run-time - loading crypto from the secret in the local namespace");

        string secretVolumeMountPath = _environmentsWrapper.SecretVolumeMountPath;
        string defaultSecretVolumeMountPath = _environmentsWrapper.DefaultSecretVolumeMountPath;

        if (string.IsNullOrWhiteSpace(secretVolumeMountPath))
        {
            var message = "cert secret path must be set.";
            _logger.Error(message);
            throw new InvalidOperationException(message);
        }
        string privateKeyPem = await ReadKeyFromMount(Path.Combine(secretVolumeMountPath, PRIVATE_KEY_FILE));
        _logger.Info($"Key Base64 decoded layer 1");
        var certificateIsValid = await CheckValidCertificate(deviceId);
        if (!certificateIsValid)
        {
            if (string.IsNullOrWhiteSpace(defaultSecretVolumeMountPath))
            {
                var message = "default cert secret path must be set.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }
            _logger.Info("certificate is not valid. go to default certificate");
            privateKeyPem = await ReadKeyFromMount(Path.Combine(defaultSecretVolumeMountPath, PRIVATE_KEY_FILE));
        }

        return LoadPrivateKeyFromPem(privateKeyPem);
    }

    private async Task<string> ReadKeyFromMount(string volumeMountPath)
    {
        using (FileStream fileStream = _fileStreamerWrapper.OpenRead(volumeMountPath))
        {
            var key = await _fileStreamerWrapper.ReadAllTextAsync(volumeMountPath);
            return key;
        }
    }

    private AsymmetricAlgorithm LoadPrivateKeyFromPem(string pemContent)
    {
        _logger.Debug($"Loading key from PEM...");
        bool isRSA = pemContent.Contains("RSA PRIVATE KEY");
        var privateKeyContent = pemContent.Replace("-----BEGIN EC PRIVATE KEY-----", "")
                                        .Replace("-----END EC PRIVATE KEY-----", "")
                                        .Replace("-----BEGIN PRIVATE KEY-----", "")
                                        .Replace("-----END PRIVATE KEY-----", "")
                                        .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                                        .Replace("-----END RSA PRIVATE KEY-----", "")
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();
        var privateKeyBytes = Convert.FromBase64String(privateKeyContent);
        _logger.Debug($"Key Base64 decoded");
        var keyReader = new ReadOnlySpan<byte>(privateKeyBytes);
        if (isRSA)
        {
            RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(keyReader, out _);
            _logger.Debug($"Imported private key");
            return rsa;
        }
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(keyReader, out _);
        _logger.Debug($"Imported private key");
        return ecdsa;
    }

    public async Task<string> SignData(byte[] data, string deviceId)
    {
        try
        {
            using (var signingPrivateKey = await GetSigningPrivateKeyAsync(deviceId))
            {
                var keyType = signingPrivateKey!.GetType();
                var signature = keyType.BaseType == typeof(RSA) ? ((RSA)signingPrivateKey!).SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1) : ((ECDsa)signingPrivateKey!).SignData(data, HashAlgorithmName.SHA512);
                return Convert.ToBase64String(signature); ;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"SignData error: {ex.Message}");
            return "";
        }
    }

    private async Task<bool> CheckValidCertificate(string deviceId)
    {
        _logger.Debug($"Checking certificate validity...");
        var publicKeyPem = await GetSigningPublicKeyAsync();

        var knownIdentitiesList = await GetKnownIdentitiesFromTwin(deviceId);
        if (knownIdentitiesList == null || knownIdentitiesList.Count == 0)
        {
            _logger.Error($"knownIdentitiesList is empty");
            return false;
        }

        var certificate = new X509Certificate2(publicKeyPem);
        var knownCertificate = await CeritficateIsknown(knownIdentitiesList, certificate);
        if (!knownCertificate)
        {
            _logger.Error($"certificate is not exists in knownIdentitiesList");
            return false;
        }
        return true;
    }

    private async Task<bool> CeritficateIsknown(List<KnownIdentities> knownIdentities, X509Certificate2 certificate)
    {
        var knownCertificate = knownIdentities.Any(x => x.Subject == certificate.Subject
                     && x.Thumbprint == certificate.Thumbprint &&
                   string.Format(x.ValidThru, "yyyy-MM-dd") == certificate.NotAfter.ToString("yyyy-MM-dd"));
        return knownCertificate;
    }

    private async Task<List<KnownIdentities>> GetKnownIdentitiesFromTwin(string deviceId)
    {
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
            var twinReported = twin.Properties.Reported.ToJson().ConvertToTwinReported();
            return twinReported.KnownIdentities;
        }
    }

    private string GetLastCertificateSection(string publicKeyPem)
    {
        _logger.Debug($"Getting last certificate section...");
        var lastIndexOfCertificateSection = publicKeyPem.LastIndexOf(BEGIN_CERTIFICATE);
        var lastSection = publicKeyPem.Substring(lastIndexOfCertificateSection);
        return lastSection;
    }

}
