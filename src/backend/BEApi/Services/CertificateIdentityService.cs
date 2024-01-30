using System.Text;
using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class CertificateIdentityService : ICertificateIdentityService
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly ITwinDiseredService _twinDiseredHandler;
    private readonly ILoggerHandler _logger;

    public CertificateIdentityService(ILoggerHandler logger, IEnvironmentsWrapper environmentsWrapper, IHttpRequestorService httpRequestorService
    , ITwinDiseredService twinDiseredHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _twinDiseredHandler = twinDiseredHandler ?? throw new ArgumentNullException(nameof(twinDiseredHandler));
    }

    public async Task HandleCertificate(string deviceId)
    {
        var certificateName = $"{DateTime.Now.ToString("yyyy_MM_dd")}{SharedConstants.CERTIFICATE_FILE_EXTENSION}";
       
        var publicKey = await GetPublicKey();
        await UploadCertificateToBlob(publicKey, certificateName, deviceId);
        await AddRecipeFordownloadCertificate(publicKey, certificateName, deviceId);
    }

    private async Task<byte[]> GetPublicKey()
    {
        _logger.Info($"GetPublicKey from keyHolder");
        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/GetPublicKey";
        var publicKey = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Get);
        _logger.Info($"GetPublicKey from keyHolder: {publicKey}");
        return publicKey;
    }

    private async Task UploadCertificateToBlob(byte[] publicKey, string certificateName, string deviceId)
    {
        try
        {
            var data = new StreamingUploadChunkEvent()
            {
                Data = publicKey,
                FileName = certificateName,
                StartPosition = 0
            };

            _logger.Info($"Send publicKey to BlobStreamer for uploading");
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/uploadStream?deviceId={deviceId}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
            _logger.Info("UploadCertificateToBlob succeeded.");
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadCertificateToBlob failed.", ex);
            throw new Exception($"UploadCertificateToBlob failed.", ex);
        }
    }

    private async Task AddRecipeFordownloadCertificate(byte[] publicKey, string certificateName, string deviceId)
    {
        _logger.Info($"preparing download action to add device twin");

        DownloadAction downloadAction = new DownloadAction()
        {
            Action = TwinActionType.SingularDownload,
            Description = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}",
            Source = certificateName,
            DestinationPath = $"./{SharedConstants.PKI_FOLDER_PATH}/{deviceId}.crt",
        };
        await _twinDiseredHandler.AddDesiredRecipeAsync(deviceId, SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME, downloadAction);
    }

}