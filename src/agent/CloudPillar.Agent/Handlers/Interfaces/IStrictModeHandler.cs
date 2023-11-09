using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IStrictModeHandler
{
    void CheckFileAccessPermissions(TwinActionType actionType, string fileName);
    string ReplaceRootById(TwinActionType actionType, string fileName);
    void CheckSizeStrictMode(TwinActionType actionType, long size, string fileName);
}