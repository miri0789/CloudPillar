
namespace CloudPillar.Agent.Entities.Twin;

public class TwinReport
{
   public string DeviceState { get; set; }
   public string AgentPlatform { get; set; }

   public ShellType[] SupportedShells { get; set; }

   public TwinReportChangeSpec ChangeSpec { get; set; }
}
