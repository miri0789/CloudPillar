
namespace Shared.Entities.Twin;

public abstract class TwinAction
{
    public TwinActionType ActionName { get; set; }
    public string Description { get; set; }
    public Guid ActionGuid { get; set; }
}
