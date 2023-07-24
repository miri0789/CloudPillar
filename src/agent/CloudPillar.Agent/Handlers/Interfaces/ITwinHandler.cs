using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActions();
    Task UpdateReportAction(int index, string twinPartName, StatusType status, float progress);
}