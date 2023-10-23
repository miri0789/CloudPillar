using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IStrictModeHandler
{
    void ValidateAuthenticationSettings();
    void CheckFileAccessPermissions(TwinActionType actionType, string fileName);
    string ReplaceRootById(string fileName);
    void CheckSizeStrictMode(long size, string fileName, TwinActionType actionType);
}