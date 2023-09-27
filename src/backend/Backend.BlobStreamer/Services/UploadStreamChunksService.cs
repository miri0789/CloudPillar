using Microsoft.Azure.Storage.Blob;
using Backend.BlobStreamer.Interfaces;
using Shared.Logger;
using Shared.Entities.Services;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace Backend.BlobStreamer.Services;

public class UploadStreamChunksService : IUploadStreamChunksService
{
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly RegistryManager _registryManager;


    public UploadStreamChunksService(ILoggerHandler logger, ICheckSumService checkSumService, IEnvironmentsWrapper environmentsWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _registryManager = RegistryManager.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }

    public async Task UploadStreamChunkAsync(Uri storageUri, string deviceId, byte[] readStream, long startPosition, string checkSum)
    {
        try
        {
            long chunkIndex = (startPosition / readStream.Length) + 1;

            _logger.Info($"BlobStreamer: Upload chunk number {chunkIndex} to {storageUri.AbsolutePath}");

            CloudBlockBlob blob = new CloudBlockBlob(storageUri);

            using (Stream inputStream = new MemoryStream(readStream))
            {
                //first chunk
                if (!blob.Exists())
                {
                    await blob.UploadFromStreamAsync(inputStream);
                }
                //continue upload the next stream chunks
                else
                {
                    MemoryStream existingData = new MemoryStream();
                    await blob.DownloadToStreamAsync(existingData);

                    existingData.Seek(startPosition, SeekOrigin.Begin);

                    await inputStream.CopyToAsync(existingData);

                    // Reset the position of existingData to the beginning
                    existingData.Seek(0, SeekOrigin.Begin);

                    // Upload the combined data to the blob
                    await blob.UploadFromStreamAsync(existingData);

                    if (!string.IsNullOrEmpty(checkSum))
                    {
                        await VerifyStreamChecksum(deviceId, checkSum, blob);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Blobstreamer UploadFromStreamAsync failed. Message: {ex.Message}");
        }
    }


    private async Task VerifyStreamChecksum(string deviceId, string originalCheckSum, CloudBlockBlob blob)
    {
        Stream azureStream = new MemoryStream();
        await blob.DownloadToStreamAsync(azureStream);

        string newCheckSum = await _checkSumService.CalculateCheckSumAsync(azureStream);
        var uploadSuccess = newCheckSum.Equals(originalCheckSum);
        await AddRecipeToDeisred(deviceId);

        if (uploadSuccess)
        {
            _logger.Debug($"Blobstreamer UploadFromStreamAsync: File uploaded successfully");
        }
        else
        {
            _logger.Debug($"Blobstreamer UploadFromStreamAsync Failed");
            await AddRecipeToDeisred(deviceId);
        }
    }

    private async Task AddRecipeToDeisred(string deviceId)
    {
        var twin = await _registryManager.GetTwinAsync(deviceId);
        string desiredJson = twin.Properties.Desired.ToJson();
        var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(desiredJson,
                new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> {
                            new TwinDesiredConverter(), new TwinActionConverter() }
                });


        // Parse the JSON twin
        var twinJson = JObject.FromObject(twin.Properties.Desired);

        // Get the value at the specified JSON path
        var keyElement = twinJson.SelectToken("changeSpec.patch.transitPackage");

        // var keyElement = twinJson.RootElement.SelectToken(keyJPath);

        if (keyElement == null)
        {
            throw new ArgumentException("Invalid JSON path specified");
        }


        // var items = twinDesired.ChangeSpec.Patch.TransitPackage.ToList();
        // var lastRecipe = items.Last();
        // items.Add(lastRecipe);

        // var ss = JsonConvert.SerializeObject(items,
        //         Formatting.None,
        //         new JsonSerializerSettings
        //         {
        //             ContractResolver = new CamelCasePropertyNamesContractResolver(),
        //             Converters = { new StringEnumConverter() },
        //             Formatting = Formatting.Indented,
        //             NullValueHandling = NullValueHandling.Ignore
        //         });

        if (keyElement.Parent?.Parent != null)
            keyElement.Parent.Parent[""] = "";

        // Update the device twin
        twin.Properties.Desired = new TwinCollection(ss.ToString());
        await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);

    }

    public async Task CreateTwinKeySignature(string deviceId, string keyPath, string signatureKey)
    {
        // Get the current device twin
        var twin = await _registryManager.GetTwinAsync(deviceId);

        // Parse the JSON twin
        var twinJson = JObject.FromObject(twin.Properties.Desired);

        // Get the value at the specified JSON path
        var keyElement = twinJson.SelectToken(keyPath);

        if (keyElement == null)
        {
            throw new ArgumentException("Invalid JSON path specified");
        }

        // Sign the value using the ES512 algorithm
        var dataToSign = Encoding.UTF8.GetBytes(keyElement.ToString());
        // var signature = _signingPrivateKey!.SignData(dataToSign, HashAlgorithmName.SHA512);

        // Convert the signature to a Base64 string
       // var signatureString = Convert.ToBase64String();

        if (keyElement.Parent?.Parent != null)
            keyElement.Parent.Parent[signatureKey] = "signatureString";

        // Update the device twin
        twin.Properties.Desired = new TwinCollection(twinJson.ToString());
        await _registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
    }
}