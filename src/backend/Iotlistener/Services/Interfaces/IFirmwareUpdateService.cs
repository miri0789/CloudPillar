using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IFirmwareUpdateService
{
    Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data);
}