
namespace CloudPillar.Agent.Entities.Twin;

public class TwinChangeSpec
{
   public string Id { get; set; }

   public TwinPatch[] Patch { get; set; }
}
