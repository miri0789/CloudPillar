using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace Backend.BEApi.Services.interfaces;

public interface IChangeSpecService
{
    Task AssignChangeSpecAsync(object changeSpec, string devices, string changeSpecKey);
    Task CreateChangeSpecKeySignatureAsync(string deviceId, string changeSignKey, TwinDesired twinDesired = null);
    Task CreateFileKeySignatureAsync(string deviceId, SignFileEvent signFileEvent);
    Task<string> SendToSignData(byte[] dataToSign, string deviceId);
}

