using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

public static class MockHelper
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

    public static Twin CreateTwinMock(TwinChangeSpec changeSpecDesired, TwinReportedChangeSpec changeSpecReported, List<TwinReportedCustomProp>? twinReportedCustomProps = null)
    {
        var desiredJson = JObject.Parse(_baseDesierd);
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinDesired()
        {
            ChangeSpec = changeSpecDesired,
        })));
        var reportedJson = JObject.Parse(_baseReported);
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(new TwinReported()
        {
            ChangeSpec = changeSpecReported,
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