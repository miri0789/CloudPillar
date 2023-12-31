
namespace Shared.Entities.Twin;

public class TwinReportedChangeSpec
{
   public string Id { get; set; }
   public Dictionary<string, TwinActionReported[]>? Patch { get; set; }
}
