using System.Security.Cryptography.X509Certificates;
using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Microsoft.WindowsAzure.Storage.Blob;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class CertificateIdentityService : ICertificateIdentityService
{
    private readonly CloudBlobContainer _container;
    private readonly ICloudStorageWrapper _cloudStorageWrapper;
    private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public CertificateIdentityService(ILoggerHandler logger, ICloudStorageWrapper cloudStorageWrapper, IEnvironmentsWrapper environmentsWrapper, ICloudBlockBlobWrapper cloudBlockBlobWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cloudStorageWrapper = cloudStorageWrapper ?? throw new ArgumentNullException(nameof(cloudStorageWrapper));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper));
        _container = _cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
    }
    
    public async Task UploadCertificateToBlob(X509Certificate2 certificate)
    {
        try
        {
            // Save the public key to a CER file
            byte[] certData = certificate.Export(X509ContentType.Cert);

            CloudBlockBlob blockBlob = _container.GetBlockBlobReference($"{certificate.Subject}.cer");
            await _cloudBlockBlobWrapper.UploadFromByteArrayAsync(blockBlob, certData, 0, certData.Length, CancellationToken.None);

        }
        catch (Exception ex)
        {
            _logger.Error($"UploadCertificateToBlob failed.", ex);
            throw new Exception($"UploadCertificateToBlob failed.", ex);
        }
    }
}