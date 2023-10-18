using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IStrictModeHandler
{
    void CheckAuthentucationMethodValue();
    void CheckRestrictedZones(TwinActionType actionType, string fileName);
}