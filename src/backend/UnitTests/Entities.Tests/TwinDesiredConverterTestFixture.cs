
using Shared.Entities.Twin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Backend.Entities.Tests;


public class TwinDesiredConverterTestFixture
{
    private TwinDesiredConverter _target;
    private JObject ExecWriteJson(TwinDesired twinDesiredObject)
    {
        var twinDesiredJson = JObject.Parse(JsonConvert.SerializeObject(twinDesiredObject,
       Formatting.None,
       new JsonSerializerSettings
       {
           ContractResolver = new CamelCasePropertyNamesContractResolver(),

           Converters = new List<JsonConverter> {
                                        new TwinDesiredConverter(),
                                        new StringEnumConverter()},

           Formatting = Formatting.Indented,
           NullValueHandling = NullValueHandling.Ignore
       }));
        return twinDesiredJson;
    }
    [SetUp]
    public void Setup()
    {
        _target = new TwinDesiredConverter();
    }
    private TwinDesired ExecReadJson(string json)
    {
        var reader = new JsonTextReader(new StringReader(json));
        return (TwinDesired)_target.ReadJson(reader, typeof(TwinDesired), null, new JsonSerializer());
    }

    [Test]
    public void ReadJson_ConvertChangeSign_ConvertCorretly()
    {
        var expectedResult = new TwinDesired()
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
        var expectedResult = new TwinDesired()
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>
                {
                    {
                        "ChangeSpec",
                        new TwinChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinAction[]>
                            {
                                { "transitPackage", new TwinAction[] { new TwinAction {  Action = TwinActionType.SingularUpload } } }
                            }
                        }
                    }
                },
        };

        var json = "{\"ChangeSpec\": {\"Id\": \"123\",\"Patch\": {\"transitPackage\": [{\"Action\":1}]}}}";
        var result = ExecReadJson(json);


        Assert.AreEqual(MockHelperEntities.EqualObjects(expectedResult.ChangeSpec, result.ChangeSpec), true);
    }

    [Test]
    public void WriteJson_SerializesTwinDesiredObjectToJson_ChangeSign()
    {
        var twinDesiredObject = new TwinDesired
        {
            ChangeSign = new Dictionary<string, string> { { "ChangeSpecSign", "XXX" } },
        };

        var twinDesiredJson = ExecWriteJson(twinDesiredObject);

        Assert.AreEqual(twinDesiredJson.GetValue("ChangeSpecSign").ToString(), "XXX");
    }

    [Test]
    public void WriteJson_SerializesTwinDesiredObjectToJson_ChangeSpec()
    {
        var twinDesiredObject = new TwinDesired
        {
            ChangeSpec = new Dictionary<string, TwinChangeSpec>
                {
                    {
                        "ChangeSpec",
                        new TwinChangeSpec
                        {
                            Id = "123",
                            Patch = new Dictionary<string, TwinAction[]>
                            {
                                { "transitPackage", new TwinAction[] { new TwinAction {  Action = TwinActionType.SingularUpload } } }
                            }
                        }
                    }
                },
        };


        var twinDesiredJson = ExecWriteJson(twinDesiredObject);

        TwinChangeSpec result = twinDesiredJson["ChangeSpec"].ToObject<TwinChangeSpec>();

        Assert.AreEqual(MockHelperEntities.EqualObjects(result, twinDesiredObject.ChangeSpec.First().Value), true);

    }
}

