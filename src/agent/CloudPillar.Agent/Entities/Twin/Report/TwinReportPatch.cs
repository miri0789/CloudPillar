
namespace CloudPillar.Agent.Entities.Twin;

public class TwinReportPatch
{
    public TwinActionReport[] PreTransitConfig { get; set; }
    public TwinActionReport[] TransitPackage { get; set; }
    public TwinActionReport[] PreInstallConfig { get; set; }
    public TwinActionReport[] InstallSteps { get; set; }
    public TwinActionReport[] PostInstallConfig { get; set; }
}
