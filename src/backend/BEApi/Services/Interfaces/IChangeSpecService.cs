using Shared.Entities.Twin;

namespace Backend.BEApi.Services.interfaces;

public interface IChangeSpecService
{
    Task AssignChangeSpecAsync(AssignChangeSpec changeSpec);
    Task CreateChangeSpecKeySignatureAsync(string deviceId, string changeSignKey, TwinDesired twinDesired = null);
    Task CreateFileKeySignatureAsync(string deviceId, string propName, int actionIndex, string changeSpecKey, string fileSign);
}

