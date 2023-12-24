
namespace Shared.Entities.Twin;

public class TwinReported
{
   public DeviceStateType? DeviceState { get; set; }
   public string AgentPlatform { get; set; }
   public ShellType[] SupportedShells { get; set; }
   public TwinReportedChangeSpec? ChangeSpec { get; set; }
   public TwinReportedChangeSpec? ChangeSpecDiagnostics { get; set; }
   public string SecretKey { get; set; }
   public List<TwinReportedCustomProp> Custom { get; set; }
   public string ChangeSign { get; set; }
   public DeviceStateType? deviceStateAfterServiceRestart { get; set; }
}
