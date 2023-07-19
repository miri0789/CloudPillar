
namespace CloudPillar.Agent.Handlers;
public interface ID2CEventHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null);
}