
namespace Shared.Entities.Twin;

public class TwinDesired
{
   public Dictionary<string, string>? ChangeSign { get; set; }
   
   public Dictionary<string, TwinChangeSpec>? ChangeSpec { get; set; }
}
