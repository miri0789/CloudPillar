
namespace Shared.Entities.Twin;

public class TwinChangeSpec
{
   public string? Id { get; set; }
   public Dictionary<string, TwinAction[]>? Patch { get; set; }
   public int Order { get; set; } = SharedConstants.DEFAULT_CHANGE_SPEC_ORDER_VALUE;
}
