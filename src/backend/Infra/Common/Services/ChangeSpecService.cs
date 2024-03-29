using System.Text;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;

namespace Backend.Infra.Common.Services;

public class ChangeSpecService : IChangeSpecService
{
    private readonly ITwinDiseredService _twinDesiredService;
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly ICommonEnvironmentsWrapper _environmentsCommonWrapper;
    private readonly IRegistryManagerWrapper _registryManagerWrapper;

    public ChangeSpecService(ITwinDiseredService twinDiseredService, IHttpRequestorService httpRequestorService, ICommonEnvironmentsWrapper environmentsCommonWrapper,
    IRegistryManagerWrapper registryManagerWrapper)
    {
        _twinDesiredService = twinDiseredService ?? throw new ArgumentNullException(nameof(twinDiseredService));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _environmentsCommonWrapper = environmentsCommonWrapper ?? throw new ArgumentNullException(nameof(environmentsCommonWrapper));
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
    }

    public async Task AssignChangeSpecAsync(object assignChangeSpec, string devices, string changeSpecKey)
    {
        ArgumentNullException.ThrowIfNull(assignChangeSpec);

        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            foreach (var deviceId in devices.Split(','))
            {
                var changeSpec = assignChangeSpec.ToString().ConvertToTwinDesired().ChangeSpec["patch"];
                foreach (var transistPackage in changeSpec.Patch)
                {
                    foreach (var actionKey in transistPackage.Value)
                    {
                        if (actionKey is DownloadAction downloadAction)
                        {
                            SignFileEvent signFileEvent = new SignFileEvent()
                            {
                                MessageType = D2CMessageType.SignFileKey,
                                ActionIndex = Array.IndexOf(transistPackage.Value.ToArray(), actionKey),
                                FileName = downloadAction.Source,
                                BufferSize = SharedConstants.SIGN_FILE_BUFFER_SIZE,
                                PropName = changeSpecKey.GetSignKeyByChangeSpec(),
                                ChangeSpecId = changeSpec.Id,
                                ChangeSpecKey = changeSpecKey
                            };
                            downloadAction.Sign = await GetFileSignAsync(deviceId, signFileEvent);
                        }
                    }
                }
                var changeSpecBytes = _twinDesiredService.GetChangeSpecDataToSign(changeSpec);
                var signature = await SendToSignData(changeSpecBytes, deviceId);
                await _twinDesiredService.AddChangeSpec(registryManager, deviceId, changeSpecKey, changeSpec, signature);
            }
        }
    }

    public async Task CreateChangeSpecKeySignatureAsync(string deviceId, string changeSignKey, TwinDesired twinDesired = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(changeSignKey);

        var changeSpecKey = changeSignKey.GetSpecKeyBySignKey();
        twinDesired = twinDesired ?? await _twinDesiredService.GetTwinDesiredAsync(deviceId);
        await GetAndSignTwinDesiredAsync(twinDesired, deviceId, changeSpecKey);
    }

    public async Task CreateFileKeySignatureAsync(string deviceId, SignFileEvent signFileEvent)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(signFileEvent.PropName);

        var twinDesired = await _twinDesiredService.GetTwinDesiredAsync(deviceId);
        var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(signFileEvent.ChangeSpecKey);
        var signature = await GetFileSignAsync(deviceId, signFileEvent);
        ((DownloadAction)twinDesiredChangeSpec.Patch[signFileEvent.PropName][signFileEvent.ActionIndex]).Sign = signature;
        await GetAndSignTwinDesiredAsync(twinDesired, deviceId, signFileEvent.ChangeSpecKey);
    }

    private async Task<string> GetFileSignAsync(string deviceId, SignFileEvent signFileEvent)
    {
        var fileSign = await GetFileBytesAsync(deviceId, signFileEvent);
        return await SendToSignData(fileSign, deviceId);
    }

    private async Task GetAndSignTwinDesiredAsync(TwinDesired twinDesired, string deviceId, string changeSpecKey)
    {
        var dataToSign = _twinDesiredService.GetChangeSpecDataToSign(twinDesired.GetDesiredChangeSpecByKey(changeSpecKey));
        var signature = await SendToSignData(dataToSign, deviceId);
        await _twinDesiredService.SignTwinDesiredAsync(twinDesired, deviceId, changeSpecKey.GetSignKeyByChangeSpec(), signature);
    }

    public async Task<byte[]> GetFileBytesAsync(string deviceId, SignFileEvent signFileEvent)
    {
        string blobRequestUrl = $"{_environmentsCommonWrapper.blobStreamerUrl}Blob/CalculateHash?deviceId={deviceId}";
        var signatureFileBytes = await _httpRequestorService.SendRequest<byte[]>(blobRequestUrl, HttpMethod.Post, signFileEvent);
        return signatureFileBytes;
    }

    public async Task<string> SendToSignData(byte[] dataToSign, string deviceId)
    {
        string requestUrl = $"{_environmentsCommonWrapper.keyHolderUrl}Signing/SignData?deviceId={deviceId}";
        var bytesSignature = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Post, dataToSign);
        var signature = Encoding.UTF8.GetString(bytesSignature);
        if (string.IsNullOrEmpty(signature))
        {
            throw new InvalidOperationException("Signature is empty");
        }
        return signature;
    }
}