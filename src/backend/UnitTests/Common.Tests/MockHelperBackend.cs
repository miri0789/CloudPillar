using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;

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



    public static Twin CreateTwinMock(Dictionary<string, TwinChangeSpec> changeSpecDesired, Dictionary<string, TwinReportedChangeSpec> changeSpecReported
      , List<TwinReportedCustomProp>? twinReportedCustomProps = null, Dictionary<string, string>? changeSign = null, List<KnownIdentities>? knownIdentities = null)
    {
        var desiredJson = JObject.Parse(_baseDesierd);

        var desired = new TwinDesired()
        {
            ChangeSpec = changeSpecDesired,
            ChangeSign = changeSign,
        };
        desiredJson.Merge(JObject.Parse(JsonConvert.SerializeObject(desired.ConvertToJObject())));

        var reportedJson = JObject.Parse(_baseReported);
        var reported = new TwinReported()
        {
            ChangeSpec = changeSpecReported,
            Custom = twinReportedCustomProps,
            KnownIdentities = knownIdentities
        };
        reportedJson.Merge(JObject.Parse(JsonConvert.SerializeObject(reported.ConvertToJObject())));
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