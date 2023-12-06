using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using CloudPillar.Agent.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class TwinActionsHandler : ITwinActionsHandler
{
    private readonly IDeviceClientWrapper _deviceClient;

    private readonly ILoggerHandler _logger;
    public TwinActionsHandler(IDeviceClientWrapper deviceClientWrapper, ILoggerHandler loggerHandler)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }
    public async Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, CancellationToken cancellationToken)
    {
        var changeSpecJson = JObject.Parse(JsonConvert.SerializeObject(changeSpec,
          Formatting.None,
          new JsonSerializerSettings
          {
              ContractResolver = new CamelCasePropertyNamesContractResolver(),
              Converters = { new StringEnumConverter() },
              Formatting = Formatting.Indented,
              NullValueHandling = NullValueHandling.Ignore
          }));
        var changeSpecKey = nameof(TwinReported.ChangeSpec);
        await _deviceClient.UpdateReportedPropertiesAsync(changeSpecKey, changeSpecJson, cancellationToken);

    }

    public async Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var twin = await _deviceClient.GetTwinAsync(cancellationToken);
                string reportedJson = twin.Properties.Reported.ToJson();
                var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
                actionsToReported.ToList().ForEach(actionToReport =>
                {
                    if (string.IsNullOrEmpty(actionToReport.ReportPartName)) return;
                    var reportedProp = typeof(TwinReportedPatch).GetProperty(actionToReport.ReportPartName);
                    var reportedValue = (TwinActionReported[])reportedProp.GetValue(twinReported.ChangeSpec.Patch);
                    reportedValue[actionToReport.ReportIndex] = actionToReport.TwinReport;
                    reportedProp.SetValue(twinReported.ChangeSpec.Patch, reportedValue);
                });
                await UpdateReportedChangeSpecAsync(twinReported.ChangeSpec);
            }
            catch (Exception ex)
            {
                _logger.Error($"UpdateReportedAction failed: {ex.Message}");
            }
        }

    }
}