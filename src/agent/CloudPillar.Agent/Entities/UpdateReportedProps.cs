using Newtonsoft.Json;

namespace CloudPillar.Agent.Entities;

public class UpdateReportedProps
{
    public List<TwinReportedCustomProp> Properties { get; set; }
}