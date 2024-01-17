using System.Security.Cryptography;
using System.Text;
using k8s;
using Backend.Keyholder.Interfaces;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Twin;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using Backend.Infra.Common.Services.Interfaces;
using Shared.Entities.Utilities;
using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices.Shared;

namespace Backend.Keyholder.Services;

public class SigningService : ISigningService
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;

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
            string SecretVolumeMountPath = _environmentsWrapper.SecretVolumeMountPath;

            if (string.IsNullOrWhiteSpace(SecretVolumeMountPath))
            {
                var message = "cert secret path must be set.";
                _logger.Error(message);
                throw new InvalidOperationException(message);
            }
            privateKeyPem = await File.ReadAllTextAsync(Path.Combine(SecretVolumeMountPath, "tls.key"));
            if (!string.IsNullOrWhiteSpace(privateKeyPem))
            {
                privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyPem));
                _logger.Info($"Key Base64 decoded layer 1");
            }
        }

        return LoadPrivateKeyFromPem(privateKeyPem);
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
        try
        {
            using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
            {
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

                var dataToSign = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twinDesired.ChangeSpec));
                twinDesired.ChangeSign = await SignData(dataToSign);

                twin.Properties.Desired = new TwinCollection(twinDesired.ConvertToJObject().ToString());
                await _registryManagerWrapper.UpdateTwinAsync(registryManager, deviceId, twin, twin.ETag);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"CreateTwinKeySignature error: {ex.Message}");
        }
    }

    public async Task<string> SignData(byte[] data)
    {
        try
        {
            using (var signingPrivateKey = await GetSigningPrivateKeyAsync())
            {
                var signature = signingPrivateKey!.SignData(data, HashAlgorithmName.SHA512);
                return Convert.ToBase64String(signature);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"SignData error: {ex.Message}");
            return "";
        }
    }

    public async Task CreateFileKeySignature(string deviceId, string propName, int actionIndex, byte[] hash, TwinPatchChangeSpec changeSpecKey)
    {
        var signature = await SignData(hash);
        await AddFileSignToDesired(deviceId, changeSpecKey, propName, actionIndex, signature);

    }

    private async Task AddFileSignToDesired(string deviceId, TwinPatchChangeSpec changeSpecKey, string propName, int actionIndex, string fileSign)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        try
        {
            using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
            {
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

                var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);
                ((DownloadAction)twinDesiredChangeSpec.Patch[propName][actionIndex]).Sign = fileSign;

                var dataToSign = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twinDesired.ChangeSpec));
                twinDesired.ChangeSign = await SignData(dataToSign);                

                var twinDesiredJson = twinDesired.ConvertToJObject().ToString();
                twin.Properties.Desired = new TwinCollection(twinDesiredJson);

                await _registryManagerWrapper.UpdateTwinAsync(registryManager, deviceId, twin, twin.ETag);
                _logger.Info($"Recipe: {actionIndex} has been successfully changed. DeviceId: {deviceId} ");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while attempting to update ChangeSpec: {ex.Message}");
        }
    }

}
