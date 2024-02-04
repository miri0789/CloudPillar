using System.Security.Cryptography;
using System.Text;
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

    public CertificateIdentityService(ILoggerHandler logger, IEnvironmentsWrapper environmentsWrapper, IHttpRequestorService httpRequestorService
    , ITwinDiseredService twinDiseredHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _twinDiseredHandler = twinDiseredHandler ?? throw new ArgumentNullException(nameof(twinDiseredHandler));
    }

    public async Task ProcessUpdatingAgentInNewCertificate(string deviceId)
    {
        try
        {
            var certificateName = $"{DateTime.Now.ToString("yyyy_MM_dd_HHmmdd")}{SharedConstants.CERTIFICATE_FILE_EXTENSION}";

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
        _logger.Info($"GetPublicKey from keyHolder");
        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/GetSigningPublicKeyAsync";
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

        var cerSign = await SignRecipe(publicKey, deviceId);
        DownloadAction downloadAction = new DownloadAction()
        {
            Action = TwinActionType.SingularDownload,
            Description = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}",
            Source = certificateName,
            Sign = Encoding.UTF8.GetString(cerSign),
            DestinationPath = $"./{SharedConstants.PKI_FOLDER_PATH}/{certificateName}",
        };
        await _twinDiseredHandler.AddDesiredRecipeAsync(deviceId, SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME, downloadAction);
    }

    private async Task<byte[]> SignRecipe(byte[] publicKey, string deviceId)
    {
        var signatureFileBytes = await GetCalculateHash(publicKey);

        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/signData?deviceId={deviceId}";
        var cerSign = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Post, signatureFileBytes);
        _logger.Info($"SignRecipe from keyHolder: {cerSign}");
        return cerSign;
    }
    private async Task UpdateChangeSpecSign(string deviceId)
    {
        var changeSignKey = SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME.GetSignKeyByChangeSpec();
        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/createTwinKeySignature?deviceId={deviceId}&changeSignKey={changeSignKey}";
        await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Get);
    }

    private async Task<byte[]> GetCalculateHash(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            try
            {
                sha256.TransformBlock(data, 0, (int)data.Length, null, 0);
                sha256.TransformFinalBlock(new byte[0], 0, 0);
                return sha256.Hash;
            }
            catch (Exception ex)
            {
                _logger.Error($"CalculateHashAsync failed.", ex);
                throw;
            }
        }
    }
}