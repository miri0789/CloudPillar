using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

public static class MockHelperBackend
{

    public static string _baseDesierd { get; } = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";
    public static string _baseReported { get; } = @"{
            '$metadata': {
                '$lastUpdated': '2023-08-29T12:30:36.4167057Z'
            },
            '$version': 1,
        }";



    public static Twin CreateTwinMock(TwinChangeSpec changeSpecDesired, TwinReportedChangeSpec changeSpecReported, TwinChangeSpec? changeSpecDiagnosticsDesired = null, TwinReportedChangeSpec? changeSpecDiagnosticsReported = null, List<TwinReportedCustomProp>? twinReportedCustomProps = null, string? changeSign = "----")
    {
        var desiredJson = JObject.Parse(_baseDesierd);
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinDesired()
        {
            ChangeSpec = changeSpecDesired,
            ChangeSpecDiagnostics = changeSpecDiagnosticsDesired,
            ChangeSign = changeSign,
        })));
        var reportedJson = JObject.Parse(_baseReported);
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinReported()
        {
            ChangeSpec = changeSpecReported,
            ChangeSpecDiagnostics = changeSpecDiagnosticsReported,
            Custom = twinReportedCustomProps
        })));
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.Indented
        };
        var twinProp = new TwinProperties()
        {
            Desired = new TwinCollection(JsonConvert.SerializeObject(desiredJson, settings)),
            Reported = new TwinCollection(JsonConvert.SerializeObject(reportedJson, settings))
        };
        return new Twin(twinProp);
    }
}