
namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null);
}