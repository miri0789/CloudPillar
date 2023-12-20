using Newtonsoft.Json;

public class TwinReportedCustomProp
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public object Value { get; set; }
}