
namespace CloudPillar.Agent.API.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null);
}