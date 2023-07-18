using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using shared.Entities.Events;

namespace CloudPillar.Agent.Handlers;
public interface ID2CEventHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null);
}