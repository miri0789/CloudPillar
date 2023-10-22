using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IStrictModeHandler
{
    void ValidateAuthenticationSettings();
    Task CheckFileAccessPermissionsAsync(TwinActionType actionType, string fileName);
    Task<string> ReplaceRootByIdAsync(string fileName);
}