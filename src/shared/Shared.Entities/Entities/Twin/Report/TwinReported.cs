
namespace Shared.Entities.Twin;

public class TwinReported
{
   public DeviceStateType? DeviceState { get; set; }
   public string AgentPlatform { get; set; }
   public ShellType[] SupportedShells { get; set; }
   public TwinReportedChangeSpec? ChangeSpec { get; set; }
}
