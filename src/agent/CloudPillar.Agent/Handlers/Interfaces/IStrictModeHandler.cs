using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IStrictModeHandler
{
    void CheckFileAccessPermissions(TwinActionType actionType, string fileName);
    string ReplaceRootById(string fileName, TwinActionType actionType);
    void CheckSizeStrictMode(long size, string fileName, TwinActionType actionType);
}