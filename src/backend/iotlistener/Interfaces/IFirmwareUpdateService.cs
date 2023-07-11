namespace iotlistener.Interfaces;

public interface IFirmwareUpdateService
{
    Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data);
}