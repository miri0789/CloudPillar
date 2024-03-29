using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using Shared.Logger;

namespace Backend.BEApi.Services;

public class CertificateIdentityService : ICertificateIdentityService
{
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly ITwinDiseredService _twinDiseredHandler;
    private readonly ILoggerHandler _logger;
    private readonly IChangeSpecService _changeSpecService;

    public CertificateIdentityService(ILoggerHandler logger, IEnvironmentsWrapper environmentsWrapper, IHttpRequestorService httpRequestorService
    , ITwinDiseredService twinDiseredHandler, IChangeSpecService changeSpecService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _twinDiseredHandler = twinDiseredHandler ?? throw new ArgumentNullException(nameof(twinDiseredHandler));
        _changeSpecService = changeSpecService ?? throw new ArgumentNullException(nameof(changeSpecService));
    }

    public async Task ProcessNewSigningCertificate(string deviceId)
    {
        try
        {
            var certificateName = $"{DateTime.Now.ToString("yyyy_MM_dd_HHmmss")}{SharedConstants.CERTIFICATE_FILE_EXTENSION}";

            var publicKey = await GetSigningPublicKeyAsync();
            await UploadCertificateToBlob(publicKey, certificateName, deviceId);
            await AddRecipeFordownloadCertificate(publicKey, certificateName, deviceId);
            await UpdateChangeSpecSign(deviceId);
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleCertificate failed.", ex);
            throw;
        }
    }

    private async Task<byte[]> GetSigningPublicKeyAsync()
    {
        _logger.Info($"start request GetSigningPublicKeyAsync from keyHolder");
        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/GetSigningPublicKeyAsync";
        var publicKey = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Get);
        _logger.Info($"end request GetSigningPublicKeyAsync from keyHolder, The result is: {publicKey}");
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
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}Blob/UploadStream?deviceId={deviceId}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
            _logger.Info("certificate with public key uploaded for blob successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadCertificateToBlob failed.", ex);
            throw new Exception($"An error occurred while upload certificate to blob.", ex);
        }
    }

    private async Task AddRecipeFordownloadCertificate(byte[] publicKey, string certificateName, string deviceId)
    {
        _logger.Info($"preparing serverIdentity download certificate action to add device twin");

        var cerSign = await SignCertificateFile(publicKey, deviceId, certificateName);
        DownloadAction downloadAction = new DownloadAction()
        {
            Action = TwinActionType.SingularDownload,
            Description = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}",
            Source = certificateName,
            Sign = cerSign,
            DestinationPath = $"./{SharedConstants.PKI_FOLDER_PATH}/{certificateName}",
        };
        await _twinDiseredHandler.AddDesiredRecipeAsync(deviceId, SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME, downloadAction, SharedConstants.MAX_CHANGE_SPEC_ORDER_VALUE);
        _logger.Info($"serverIdentity download certificate action added to device twin successfully.");
    }

    private async Task<string> SignCertificateFile(byte[] publicKey, string deviceId, string certificateName)
    {
        _logger.Info($"Send request to get Sign certificate from keyHolder");

        var signFileEvent = new SignFileEvent()
        {
            BufferSize = SharedConstants.SIGN_FILE_BUFFER_SIZE,
            FileName = certificateName,
            ChangeSpecId = String.Empty,
            ChangeSpecKey = String.Empty,
            PropName = String.Empty
        };
        var signatureFileBytes = await _changeSpecService.GetFileBytesAsync(deviceId, signFileEvent);

        var cerSign = await _changeSpecService.SendToSignData(signatureFileBytes, deviceId);
        _logger.Info($"Sign certificate from keyHolder: {cerSign}");
        return cerSign;
    }

    private async Task UpdateChangeSpecSign(string deviceId)
    {
        var changeSignKey = SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME.GetSignKeyByChangeSpec();
        await _changeSpecService.CreateChangeSpecKeySignatureAsync(deviceId, changeSignKey);
    }        
}