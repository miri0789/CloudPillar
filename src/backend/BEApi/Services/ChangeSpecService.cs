using Backend.BEApi.Services.interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Shared.Entities.Twin;

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

    public async Task AssignChangeSpecAsync(AssignChangeSpec changeSpec)
    {
        ArgumentNullException.ThrowIfNull(changeSpec);

        foreach (var device in changeSpec.Devices.Split(','))
        {
            var twinDesired = await _twinDiseredService.AddChangeSpec(device, changeSpec);
            var dataToSign = await _twinDiseredService.GetTwinDesiredDataToSign(device, changeSpec.ChangeSpecKey);
            var signature = await SendTwinDesiredToSign(device, dataToSign);
            await _twinDiseredService.SignTwinDesiredAsync(twinDesired, device, changeSpec.ChangeSpecKey, signature);
        }
    }

    private async Task<string> SendTwinDesiredToSign(string deviceId, byte[] dataToSign)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(dataToSign);

        string requestUrl = $"{_environmentsWrapper.keyHolderUrl}Signing/SignData";
        var bytesSignature = await _httpRequestorService.SendRequest<byte[]>(requestUrl, HttpMethod.Post, dataToSign);
        return Convert.ToBase64String(bytesSignature);
    }
}