
namespace Shared.Entities.Twin;

public class TwinDesired
{
   public IDictionary<string, string>? ChangeSign { get; set; }
   
   public IDictionary<string, TwinChangeSpec>? ChangeSpec { get; set; }
}
