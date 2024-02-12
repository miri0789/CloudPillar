
using Shared.Entities.Twin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Backend.Entities.Tests;


public class TwinReportedConverterTestFixture
{
    private TwinReportedConverter _target;

    [SetUp]
    public void Setup()
    {
        _target = new TwinReportedConverter();
    }
    private TwinReported ExecReadJson(string json)
    {
        var reader = new JsonTextReader(new StringReader(json));
        return (TwinReported)_target.ReadJson(reader, typeof(TwinReported), null, new JsonSerializer());
    }

    private JObject ExecWriteJson(TwinReported twinReportedObject)
    {
        var twinReportedJson = JObject.Parse(JsonConvert.SerializeObject(twinReportedObject,
      Formatting.None,
      new JsonSerializerSettings
      {
          ContractResolver = new CamelCasePropertyNamesContractResolver(),
          Converters = new List<JsonConverter> {
                                        new TwinReportedConverter(), new StringEnumConverter() },
          Formatting = Formatting.Indented,
          NullValueHandling = NullValueHandling.Ignore
      }));
        return twinReportedJson;
    }


    [Test]
    public void ReadJson_ConvertChangeSign_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" }, { "ChangeSpecTestSign", "YYY" } },
        };

        var json = "{\"ChangeSpecSign\": \"XXX\",\"ChangeSpecTestSign\": \"YYY\"}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.ChangeSign, result.ChangeSign), true);
    }

    [Test]
    public void ReadJson_ConvertChangeSpec_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            ChangeSpec = new Dictionary<string, TwinReportedChangeSpec>
                {
                    {
                        "ChangeSpec",
                        new TwinReportedChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinActionReported[]>
                            {
                                { "transitPackage", new TwinActionReported[] { new TwinActionReported {  Status = StatusType.Success } } }
                            }
                        }
                    }
                },
        };

        var json = "{\"ChangeSpec\": {  \"Id\": \"123\",  \"Patch\": {\"transitPackage\": [  {\"Status\": 5}]}}}";
        var result = ExecReadJson(json);


        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.ChangeSpec, result.ChangeSpec), true);
    }

    [Test]
    public void ReadJson_ConvertDeviceState_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            DeviceState = DeviceStateType.Ready,
        };

        var json = "{\"DeviceState\": 2}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.DeviceState, result.DeviceState), true);
    }

    [Test]
    public void ReadJson_ConvertAgentPlatform_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            AgentPlatform = "Windows",
        };

        var json = "{\"AgentPlatform\": \"Windows\"}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.AgentPlatform, result.AgentPlatform), true);
    }


    [Test]
    public void ReadJson_ConvertSupportedShells_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            SupportedShells = new[] { ShellType.Powershell, ShellType.Bash },
        };

        var json = "{ \"SupportedShells\": [0,2]}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.SupportedShells, result.SupportedShells), true);
    }


    [Test]
    public void ReadJson_ConvertSecretKey_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            SecretKey = "Secret123",
        };

        var json = "{ \"SecretKey\": \"Secret123\"}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.SecretKey, result.SecretKey), true);
    }

    [Test]
    public void ReadJson_ConvertCustom_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            Custom = new Dictionary<string, object>() { { "Custom1", "Value1" }, { "Custom2", "Value2" } }
        };

        var json = "{\"Custom\":{ \"Custom1\": \"Value1\", \"Custom2\": \"Value2\"}}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.Custom, result.Custom), true);
    }

    [Test]
    public void ReadJson_ConvertChangeSpecId_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            ChangeSpecId = "ChangeSpec123",
        };

        var json = "{ \"ChangeSpecId\": \"ChangeSpec123\"}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.ChangeSpecId, result.ChangeSpecId), true);
    }

    [Test]
    public void ReadJson_ConvertCertificateValidity_ConvertCorretly()
    {
        var DateTimeUtcNow = DateTime.UtcNow.Date;
        var ExpirationDate = DateTime.UtcNow.Date.AddDays(30);
        var expectedResult = new TwinReported()
        {
            CertificateValidity = new CertificateValidity { CreationDate = DateTimeUtcNow, ExpirationDate = ExpirationDate },
        };

        var json = "{ \"CertificateValidity\": {\"CreationDate\": \"" + DateTimeUtcNow + "\", \"ExpirationDate\": \"" + ExpirationDate + "\"}}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.CertificateValidity, result.CertificateValidity), true);
    }

    [Test]
    public void ReadJson_ConvertDeviceStateAfterServiceRestart_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            DeviceStateAfterServiceRestart = DeviceStateType.Uninitialized,
        };

        var json = "{ \"DeviceStateAfterServiceRestart\": 0}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.DeviceStateAfterServiceRestart, result.DeviceStateAfterServiceRestart), true);
    }

    [Test]
    public void ReadJson_ConvertKnownIdentities_ConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            KnownIdentities = new List<KnownIdentities>
                {
                    new KnownIdentities("Subject1",  "Thumbprint1",  "2024-01-31" ),
                    new KnownIdentities ("Subject2",  "Thumbprint2",  "2024-02-29" )
                }
        };

        var json = "{ \"KnownIdentities\": [{\"Subject\": \"Subject1\", \"Thumbprint\": \"Thumbprint1\", \"ValidThru\": \"2024-01-31\"},{\"Subject\": \"Subject2\", \"Thumbprint\": \"Thumbprint2\", \"ValidThru\": \"2024-02-29\"}]}";
        var result = ExecReadJson(json);

        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.KnownIdentities, result.KnownIdentities), true);
    }

    [Test]
    public void WriteJson_SerializesTwinReportedObjectToJson_ChangeSign()
    {
        var twinReportedObject = new TwinReported
        {
            ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" } },
        };

        var twinReportedJson = ExecWriteJson(twinReportedObject);

        Assert.AreEqual(twinReportedJson.GetValue("ChangeSpecSign").ToString(), "XXX");
    }


    [Test]
    public void WriteJson_SerializesTwinReportedObjectToJson_ChangeSpec()
    {
        var twinReportedObject = new TwinReported
        {
            ChangeSpec = new Dictionary<string, TwinReportedChangeSpec>
                {
                    {
                        "ChangeSpec",
                        new TwinReportedChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinActionReported[]>
                            {
                                { "transitPackage", new TwinActionReported[] { new TwinActionReported {  Status = StatusType.Success } } }
                            }
                        }
                    }
                },
        };

        var twinReportedJson = ExecWriteJson(twinReportedObject);

        TwinReportedChangeSpec result = twinReportedJson["ChangeSpec"].ToObject<TwinReportedChangeSpec>();

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, twinReportedObject.ChangeSpec.First().Value), true);
    }


    [Test]
    public void WriteJson_SerializesTwinReportedObjectToJson_AnotherProperties()
    {
        var twinReportedObject = new TwinReported
        {
            KnownIdentities = new List<KnownIdentities>
                {
                    new KnownIdentities("Subject1",  "Thumbprint1",  "2024-01-31" ),
                    new KnownIdentities ("Subject2",  "Thumbprint2",  "2024-02-29" )
                },
        };

        var twinReportedJson = ExecWriteJson(twinReportedObject);

        List<KnownIdentities> result = twinReportedJson["KnownIdentities"].ToObject<List<KnownIdentities>>();

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, twinReportedObject.KnownIdentities), true);

    }
}

