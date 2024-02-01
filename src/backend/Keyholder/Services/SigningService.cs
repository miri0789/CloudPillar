using System.Security.Cryptography;
using System.Text;
using Backend.Keyholder.Interfaces;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;

namespace Backend.Keyholder.Services;

public class SigningService : ISigningService
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;

    private const string PRIVATE_KEY_FILE = "tls.key";


    public SigningService(IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger, IRegistryManagerWrapper registryManagerWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        if (string.IsNullOrEmpty(_environmentsWrapper.iothubConnectionString))
        {
            throw new ArgumentNullException(nameof(_environmentsWrapper.iothubConnectionString));
        }
    }


    private async Task<AsymmetricAlgorithm> GetSigningPrivateKeyAsync()
    {
        _logger.Info("Loading signing crypto key...");
        string privateKeyPem = _environmentsWrapper.signingPem;
        if (!string.IsNullOrWhiteSpace(privateKeyPem))
        {
            privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyPem));
            _logger.Info($"Key Base64 decoded layer 1");
        }
        else
        {
            _logger.Info("In kube run-time - loading crypto from the secret in the local namespace.");
            string SecretVolumeMountPath = _environmentsWrapper.SecretVolumeMountPath;

            if (string.IsNullOrWhiteSpace(SecretVolumeMountPath))
            {
                var message = "cert secret path must be set.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }
            privateKeyPem = await File.ReadAllTextAsync(Path.Combine(SecretVolumeMountPath, PRIVATE_KEY_FILE));
            _logger.Info($"Key Base64 decoded layer 1");
        }

        return LoadPrivateKeyFromPem(privateKeyPem);
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
        ecdsa.ImportPkcs8PrivateKey(keyReader, out _);
        _logger.Debug($"Imported private key");
        return ecdsa;
    }

    public async Task<byte[]> SignData(byte[] data)
    {
        try
        {
            using (var signingPrivateKey = await GetSigningPrivateKeyAsync())
            {
                var keyType = signingPrivateKey!.GetType();
                var signature = keyType.BaseType == typeof(RSA) ? ((RSA)signingPrivateKey!).SignData(data, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1) : ((ECDsa)signingPrivateKey!).SignData(data, HashAlgorithmName.SHA512);
                var r = Convert.ToBase64String(signature);
                return signature;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"SignData error: {ex.Message}");
            return new byte[0];
        }
    }
}
