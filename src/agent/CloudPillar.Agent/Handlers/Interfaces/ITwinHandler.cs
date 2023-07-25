using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActions();
    Task UpdateReportAction(ActionToReport actionToReport);
    Task InitReportDeviceParams();
}