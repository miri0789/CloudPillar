﻿using System.Security.Cryptography;
using System.Text;
using k8s;
using Backend.Keyholder.Interfaces;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Twin;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Backend.Infra.Common.Services.Interfaces;


namespace Backend.Keyholder.Services;


public class SigningService : ISigningService
{
    private ECDsa _signingPrivateKey;
    private readonly RegistryManager _registryManager;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ITwinDiseredService _twinDesiredService;
    private readonly ILoggerHandler _logger;

    public SigningService(IEnvironmentsWrapper environmentsWrapper, ITwinDiseredService twinDiseredService, ILoggerHandler logger)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _twinDesiredService = twinDiseredService ?? throw new ArgumentNullException(nameof(twinDiseredService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrEmpty(_environmentsWrapper.iothubConnectionString))
        {
            throw new ArgumentNullException(nameof(_environmentsWrapper.iothubConnectionString));
        }
        _registryManager = RegistryManager.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }

    public async Task Init()
    {
        _signingPrivateKey = await GetSigningPrivateKeyAsync();
    }


    private async Task<ECDsa> GetSigningPrivateKeyAsync()
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
            string secretName = _environmentsWrapper.secretName;
            string secretKey = _environmentsWrapper.secretKey;

            if (string.IsNullOrWhiteSpace(secretName) || string.IsNullOrWhiteSpace(secretKey))
            {
                var message = "Private key secret name and secret key must be set.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }
            privateKeyPem = await GetPrivateKeyFromK8sSecretAsync(secretName, secretKey);
        }

        return LoadPrivateKeyFromPem(privateKeyPem);
    }

    private async Task<string> GetPrivateKeyFromK8sSecretAsync(string secretName, string secretKey, string? secretNamespace = null)
    {
        _logger.Debug($"GetPrivateKeyFromK8sSecretAsync {secretName}, {secretKey}, {secretNamespace}");
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        var k8sClient = new Kubernetes(config);
        _logger.Debug($"Got k8s client in namespace {config.Namespace}");

        var ns = String.IsNullOrWhiteSpace(secretNamespace) ? config.Namespace : secretNamespace;
        if (String.IsNullOrWhiteSpace(ns))
        {
            throw new Exception("k8s namespace not found.");
        }

        var secrets = await k8sClient.ListNamespacedSecretAsync(ns);

        _logger.Debug($"Secrets in namespace '{ns}':");
        foreach (var secret in secrets.Items)
        {
            _logger.Debug($"- {secret.Metadata.Name}");
        }

        var targetSecret = await k8sClient.ReadNamespacedSecretAsync(secretName, ns);
        _logger.Debug($"Got k8s secret");

        if (targetSecret.Data.TryGetValue(secretKey, out var privateKeyBytes))
        {
            _logger.Debug($"Got k8s secret bytes");
            return Encoding.UTF8.GetString(privateKeyBytes);
        }

        throw new Exception("Private key not found in the Kubernetes secret.");
    }

    private ECDsa LoadPrivateKeyFromPem(string pemContent)
    {
        _logger.Debug($"Loading key from PEM...");
        var privateKeyContent = pemContent.Replace("-----BEGIN EC PRIVATE KEY-----", "")
                                        .Replace("-----END EC PRIVATE KEY-----", "")
                                        .Replace("-----BEGIN PRIVATE KEY-----", "")
                                        .Replace("-----END PRIVATE KEY-----", "")
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();
        var privateKeyBytes = Convert.FromBase64String(privateKeyContent);
        _logger.Debug($"Key Base64 decoded");
        var keyReader = new ReadOnlySpan<byte>(privateKeyBytes);
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(keyReader, out _);
        _logger.Debug($"Imported private key");
        return ecdsa;
    }


    public async Task CreateTwinKeySignature(string deviceId)
    {
        if (_signingPrivateKey == null)
        {
            await Init();
        }

        // Get the current device twin
        var twin = await _registryManager.GetTwinAsync(deviceId);

        // Parse the JSON twin
        var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

        // Sign the value using the ES512 algorithm
        var dataToSign = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twinDesired.ChangeSpec));
        var signature = _signingPrivateKey!.SignData(dataToSign, HashAlgorithmName.SHA512);

        // Convert the signature to a Base64 string
        var signatureString = Convert.ToBase64String(signature);

        twinDesired.ChangeSign = signatureString;
        // Update the device twin
        twin.Properties.Desired = new TwinCollection(twinDesired.ConvertToJObject().ToString());
        await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
    }

    public async Task CreateFileKeySignature(string deviceId, string actionId, byte[] hash)
    {
        if (_signingPrivateKey == null)
        {
            await Init();
        }
        var signature = _signingPrivateKey.SignHash(hash);
        await _twinDesiredService.ChangeDesiredRecipeAsync(deviceId, TwinPatchChangeSpec.ChangeSpec, actionId, Convert.ToBase64String(signature));
    }
}
