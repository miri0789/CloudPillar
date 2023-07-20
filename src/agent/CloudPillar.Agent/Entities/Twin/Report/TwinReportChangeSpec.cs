
namespace CloudPillar.Agent.Entities.Twin;

public class TwinReportChangeSpec
{
   public string Id { get; set; }

   public TwinReportPatch[] Patch { get; set; }
}
