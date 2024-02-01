using System.Text;
using Backend.BEApi.Services.interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;

namespace Backend.BEApi.Services;

public class ChangeSpecService : IChangeSpecService
{
    private readonly ITwinDiseredService _twinDiseredService;
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    public ChangeSpecService(ITwinDiseredService twinDiseredService, IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper)
    {
        _twinDiseredService = twinDiseredService ?? throw new ArgumentNullException(nameof(twinDiseredService));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
    }

    public async Task AssignChangeSpecAsync(object changeSpec, string devices, string changeSpecKey)
    {
        ArgumentNullException.ThrowIfNull(changeSpec);

        foreach (var deviceId in devices.Split(','))
        {
            await _twinDiseredService.AddChangeSpec(deviceId, changeSpecKey, changeSpec);
        }
    }

    public async Task CreateChangeSpecKeySignatureAsync(string deviceId, string changeSignKey, TwinDesired twinDesired = null)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(changeSignKey);

        var changeSpecKey = changeSignKey.GetSpecKeyBySignKey();
        twinDesired = twinDesired ?? await _twinDiseredService.GetTwinDesiredAsync(deviceId);
        await GetAndSignTwinDesiredAsync(twinDesired, deviceId, changeSpecKey);
    }

    public async Task CreateFileKeySignatureAsync(string deviceId, string propName, int actionIndex, string changeSpecKey, string fileSign)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(propName);
        ArgumentNullException.ThrowIfNull(changeSpecKey);

        var signature = await SendToSignData(Convert.FromBase64String(fileSign));
        var twinDesired = await _twinDiseredService.GetTwinDesiredAsync(deviceId);
        var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);
        ((DownloadAction)twinDesiredChangeSpec.Patch[propName][actionIndex]).Sign = signature;

        await GetAndSignTwinDesiredAsync(twinDesired, deviceId, changeSpecKey);
    }

    private async Task GetAndSignTwinDesiredAsync(TwinDesired twinDesired, string deviceId, string changeSpecKey)
    {
        var dataToSign = _twinDiseredService.GetTwinDesiredDataToSign(twinDesired, changeSpecKey);
        var signature = await SendToSignData(dataToSign);
        await _twinDiseredService.SignTwinDesiredAsync(twinDesired, deviceId, changeSpecKey.GetSignKeyByChangeSpec(), signature);
    }

    private async Task<string> SendToSignData(byte[] dataToSign)
    {
        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/SignData";
        var bytesSignature = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Post, dataToSign);
        return Convert.ToBase64String(bytesSignature);
    }
}