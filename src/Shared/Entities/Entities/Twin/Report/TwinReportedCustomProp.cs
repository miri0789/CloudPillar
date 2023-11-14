using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TwinReportedCustomProp
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public object Value { get; set; }
}