namespace Shared.Entities.Twin;

public class AssignChangeSpec : TwinAction
{
    public TwinChangeSpec ChangeSpec { get; set; }
    public string? ChangeSpecKey { get; set; }
    public string Devices { get; set; }
}