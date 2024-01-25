
using Shared.Entities.Twin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend.Entities.Tests;


public class TwinReportedConverterTestFixture
{
    private TwinReportedConverter _target;

    [SetUp]
    public void Setup()
    {
        _target = new TwinReportedConverter();
    }
    private TwinReported ExexReadJson(string json)
    {
        var reader = new JsonTextReader(new StringReader(json));
        return (TwinReported)_target.ReadJson(reader, typeof(TwinReported), null, new JsonSerializer());
    }

    [Test]
    public void ReadJson_ReadJson_ChangeSignConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" }, { "ChangeSpecTestSign", "YYY" } },
        };

        var json = "{\"ChangeSpecSign\": \"XXX\",\"ChangeSpecTestSign\": \"YYY\"}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.ChangeSign, result.ChangeSign), true);
    }

    [Test]
    public void ReadJson_ReadJson_ChangeSpecConvertCorretly()
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
        var result = ExexReadJson(json);


        Assert.AreEqual(EqualObjects(expectedResult.ChangeSpec, result.ChangeSpec), true);
    }

    [Test]
    public void ReadJson_ReadJson_DeviceStateConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            DeviceState = DeviceStateType.Ready,
        };

        var json = "{\"DeviceState\": 2}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.DeviceState, result.DeviceState), true);
    }

    [Test]
    public void ReadJson_ReadJson_AgentPlatformConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            AgentPlatform = "Windows",
        };

        var json = "{\"AgentPlatform\": \"Windows\"}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.AgentPlatform, result.AgentPlatform), true);
    }


    [Test]
    public void ReadJson_ReadJson_SupportedShellsConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            SupportedShells = new[] { ShellType.Powershell, ShellType.Bash },
        };

        var json = "{ \"SupportedShells\": [0,2]}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.SupportedShells, result.SupportedShells), true);
    }


    [Test]
    public void ReadJson_ReadJson_SecretKeyConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            SecretKey = "Secret123",
        };

        var json = "{ \"SecretKey\": \"Secret123\"}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.SecretKey, result.SecretKey), true);
    }

    [Test]
    public void ReadJson_ReadJson_CustomConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            Custom = new List<TwinReportedCustomProp>
                {
                    new TwinReportedCustomProp { Name = "Custom1", Value = "Value1" },
                    new TwinReportedCustomProp { Name = "Custom2", Value = "Value2" }
                }
        };

        var json = "{ \"Custom\": [{\"Name\": \"Custom1\", \"Value\": \"Value1\"},{\"Name\": \"Custom2\", \"Value\": \"Value2\"}]}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.Custom, result.Custom), true);
    }

    [Test]
    public void ReadJson_ReadJson_ChangeSpecIdConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            ChangeSpecId = "ChangeSpec123",
        };

        var json = "{ \"ChangeSpecId\": \"ChangeSpec123\"}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.ChangeSpecId, result.ChangeSpecId), true);
    }

    [Test]
    public void ReadJson_ReadJson_CertificateValidityConvertCorretly()
    {
        var DateTimeUtcNow = DateTime.UtcNow;
        var ExpirationDate = DateTime.UtcNow.AddDays(30);
        var expectedResult = new TwinReported()
        {
            CertificateValidity = new CertificateValidity { CreationDate = DateTimeUtcNow, ExpirationDate = ExpirationDate },
        };

        var json = "{ \"CertificateValidity\": {\"CreationDate\": \"" + DateTimeUtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") + "\", \"ExpirationDate\": \"" + ExpirationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ") + "\"}}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.CertificateValidity, result.CertificateValidity), true);
    }

    [Test]
    public void ReadJson_ReadJson_DeviceStateAfterServiceRestartConvertCorretly()
    {
        var expectedResult = new TwinReported()
        {
            DeviceStateAfterServiceRestart = DeviceStateType.Uninitialized,
        };

        var json = "{ \"DeviceStateAfterServiceRestart\": 0}";
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.DeviceStateAfterServiceRestart, result.DeviceStateAfterServiceRestart), true);
    }

    [Test]
    public void ReadJson_ReadJson_KnownIdentitiesConvertCorretly()
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
        var result = ExexReadJson(json);

        Assert.AreEqual(EqualObjects(expectedResult.KnownIdentities, result.KnownIdentities), true);
    }

    [Test]
    public void WriteJson_SerializesTwinReportedObjectToJson_ChangeSign()
    {
        var twinReportedObject = new TwinReported
        {
            ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" }, { "ChangeSpecTestSign", "YYY" } },
        };

        var twinDesiredJson = JObject.Parse(JsonConvert.SerializeObject(twinReportedObject,
       Formatting.None,
       new JsonSerializerSettings
       {
           Converters = new List<JsonConverter> {
                                        new TwinDesiredConverter() },
       }));

        Assert.AreEqual(EqualObjects(twinDesiredJson["ChangeSign"], twinReportedObject.ChangeSign), true);
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

        var twinDesiredJson = JObject.Parse(JsonConvert.SerializeObject(twinReportedObject,
       Formatting.None,
       new JsonSerializerSettings
       {
           Converters = new List<JsonConverter> {
                                        new TwinDesiredConverter() },
       }));

        Assert.AreEqual(EqualObjects(twinDesiredJson["ChangeSpec"], twinReportedObject.ChangeSpec), true);
    }
    private bool EqualObjects(object expectedResult, object result)
    {
        return JsonConvert.SerializeObject(expectedResult) == JsonConvert.SerializeObject(result);
    }
   
    // public string converJsonToString(TwinReported twinReported)
    // {
    //     // var twinReported = new TwinReported
    //     // {
    //     //     DeviceState = DeviceStateType.Ready,
    //     //     AgentPlatform = "Windows",
    //     //     SupportedShells = new[] { ShellType.Powershell, ShellType.Bash },
    //     //     ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" } },
    //     //     ChangeSpec = new Dictionary<string, TwinReportedChangeSpec>
    //     //         {
    //     //             {
    //     //                 "ChangeSpec",
    //     //                 new TwinReportedChangeSpec
    //     //                 {
    //     //                     Id = "123",
    //     //                     Patch = new Dictionary<string, TwinActionReported[]>
    //     //                     {
    //     //                         { "transitPackage", new TwinActionReported[] { new TwinActionReported {  Status = StatusType.Success } } }
    //     //                     }
    //     //                 }
    //     //             }
    //     //         },
    //     //     SecretKey = "Secret123",
    //     //     Custom = new List<TwinReportedCustomProp>
    //     //         {
    //     //             new TwinReportedCustomProp { Name = "Custom1", Value = "Value1" },
    //     //             new TwinReportedCustomProp { Name = "Custom2", Value = "Value2" }
    //     //         },
    //     //     ChangeSpecId = "ChangeSpec123",
    //     //     CertificateValidity = new CertificateValidity { CreationDate = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(30) },
    //     //     DeviceStateAfterServiceRestart = DeviceStateType.Ready,
    //     //     KnownIdentities = new List<KnownIdentities>
    //     //         {
    //     //             new KnownIdentities("Subject1",  "Thumbprint1",  "2024-01-31" ),
    //     //             new KnownIdentities ("Subject2",  "Thumbprint2",  "2024-02-29" )
    //     //         }
    //     // };

    //     // Serialize the object to a JSON string
    //     var json = JsonConvert.SerializeObject(twinReported, Formatting.Indented).ToString().Replace("\r\n", "");

    //     // Print the generated JSON string
    //     return json;
    // }
}

