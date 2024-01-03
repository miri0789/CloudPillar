
namespace Shared.Entities.Twin;

public class TwinChangeSpec
{
   public string? Id { get; set; }
   public Dictionary<string, TwinAction[]>? Patch { get; set; }
}
