using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Events;


namespace CloudPillar.Agent.Handlers;

public class TwinHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly DeviceClient _deviceClient;
    public TwinHandler(IDeviceClientWrapper deviceClientWrapper, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

        _deviceClientWrapper = deviceClientWrapper;
        _deviceClient = deviceClientWrapper.CreateDeviceClient(environmentsWrapper.deviceConnectionString);
    }


        public static async Task UpdateDeviceState(DeviceClient deviceClient, string deviceState) 
        {
                var currentTwin = await deviceClient.GetTwinAsync();

                var desiredJObject = JObject.Parse(currentTwin.Properties.Desired.ToJson());
                var reportedJObject = JObject.Parse(currentTwin.Properties.Reported.ToJson());

                reportedJObject["deviceState"] = deviceState;
                await UpdateReportedPropertiesAsync(deviceClient, "deviceState", deviceState);
                string agentPlatform = RuntimeInformation.OSDescription;
                reportedJObject["agentPlatform"] = agentPlatform;
                await UpdateReportedPropertiesAsync(deviceClient, "agentPlatform", agentPlatform);
                JArray shells = JArray.FromObject(GetSupportedShells());
                reportedJObject["supportedShells"] = shells;
                await UpdateReportedPropertiesAsync(deviceClient, "supportedShells", shells);
        }

}