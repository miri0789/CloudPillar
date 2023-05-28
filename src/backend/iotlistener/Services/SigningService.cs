using System.Security.Cryptography;
using System.Text;
using k8s;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace iotlistener;


public interface ISigningService
{
    Task Init();

    Task CreateTwinKeySignature(string deviceId, SignEvent signEvent);
}

public class SigningService : ISigningService
{
    private ECDsa _signingPrivateKey;
    private readonly RegistryManager _registryManager;

    public SigningService()
    {
        _registryManager = RegistryManager.CreateFromConnectionString(Environment.GetEnvironmentVariable(Constants.iothubConnectionString));
    }

    public async Task Init()
    {
        _signingPrivateKey = await GetSigningPrivateKeyAsync();
    }


    private static async Task<ECDsa> GetSigningPrivateKeyAsync()
    {
        Console.WriteLine("Loading signing crypto key...");
        string? privateKeyPem = null;
        bool IsInCluster = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Constants.kubernetesServiceHost));
        if (IsInCluster)
        {
            privateKeyPem = Environment.GetEnvironmentVariable(Constants.signingPem);
            if (!string.IsNullOrEmpty(privateKeyPem))
            {
                privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyPem));
                Console.WriteLine($"Key Base64 decoded layer 1");
            }
            else
            {
                Console.WriteLine("In kube run-time - loading crypto from the secret in the local namespace.");
                string secretName = Environment.GetEnvironmentVariable(Constants.secretName);
                string secretKey = Environment.GetEnvironmentVariable(Constants.secretKey);

                if (string.IsNullOrEmpty(secretName) || string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("Private key secret name and secret key must be set.");
                }
                privateKeyPem = await GetPrivateKeyFromK8sSecretAsync(secretName, secretKey);
            }
        }
        else
        {
            Console.WriteLine("Not in kube run-time - loading crypto from the local storage.");
            // Load the private key from a local file when running locally
            privateKeyPem = await File.ReadAllTextAsync("dbg/sign-privkey.pem");
        }

        return LoadPrivateKeyFromPem(privateKeyPem);
    }

    private static async Task<string> GetPrivateKeyFromK8sSecretAsync(string secretName, string secretKey, string? secretNamespace = null)
    {
        Console.WriteLine($"GetPrivateKeyFromK8sSecretAsync {secretName}, {secretKey}, {secretNamespace}");
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        var k8sClient = new Kubernetes(config);
        Console.WriteLine($"Got k8s client in namespace {config.Namespace}");

        var ns = String.IsNullOrEmpty(secretNamespace) ? config.Namespace : secretNamespace;
        var secrets = await k8sClient.ListNamespacedSecretAsync(ns);

        Console.WriteLine($"Secrets in namespace '{ns}':");
        foreach (var secret in secrets.Items)
        {
            Console.WriteLine($"- {secret.Metadata.Name}");
        }

        var targetSecret = await k8sClient.ReadNamespacedSecretAsync(secretName, ns);
        Console.WriteLine($"Got k8s secret");

        if (targetSecret.Data.TryGetValue(secretKey, out var privateKeyBytes))
        {
            Console.WriteLine($"Got k8s secret bytes");
            return Encoding.UTF8.GetString(privateKeyBytes);
        }

        throw new Exception("Private key not found in the Kubernetes secret.");
    }

    private static ECDsa LoadPrivateKeyFromPem(string pemContent)
    {
        Console.WriteLine($"Loading key from PEM...");
        var privateKeyContent = pemContent.Replace("-----BEGIN EC PRIVATE KEY-----", "")
                                        .Replace("-----END EC PRIVATE KEY-----", "")
                                        .Replace("-----BEGIN PRIVATE KEY-----", "")
                                        .Replace("-----END PRIVATE KEY-----", "")
                                        .Replace("\n", "")
                                        .Replace("\r", "")
                                        .Trim();
        var privateKeyBytes = Convert.FromBase64String(privateKeyContent);
        Console.WriteLine($"Key Base64 decoded");
        var keyReader = new ReadOnlySpan<byte>(privateKeyBytes);
        ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(keyReader, out _);
        Console.WriteLine($"Imported private key");
        return ecdsa;
    }


    public async Task CreateTwinKeySignature(string deviceId, SignEvent signEvent)
    {
        // Get the current device twin
        var twin = await _registryManager.GetTwinAsync(deviceId);

        // Parse the JSON twin
        var twinJson = JObject.FromObject(twin.Properties.Desired);

        // Get the value at the specified JSON path
        var keyElement = twinJson.SelectToken(signEvent.keyPath);

        if (keyElement == null)
        {
            throw new ArgumentException("Invalid JSON path specified");
        }

        // Sign the value using the ES512 algorithm
        var dataToSign = Encoding.UTF8.GetBytes(keyElement.ToString());
        var signature = _signingPrivateKey!.SignData(dataToSign, HashAlgorithmName.SHA512);

        // Convert the signature to a Base64 string
        var signatureString = Convert.ToBase64String(signature);

        if (keyElement.Parent?.Parent != null)
            keyElement.Parent.Parent[signEvent.signatureKey] = signatureString;

        // Update the device twin
        twin.Properties.Desired = new TwinCollection(twinJson.ToString());
        await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
    }

}
