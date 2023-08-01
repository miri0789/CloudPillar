
namespace Shared.Entities.Twin;

public class TwinReportedPatch
{
    public TwinActionReported[] PreTransitConfig { get; set; }
    public TwinActionReported[] TransitPackage { get; set; }
    public TwinActionReported[] PreInstallConfig { get; set; }
    public TwinActionReported[] InstallSteps { get; set; }
    public TwinActionReported[] PostInstallConfig { get; set; }
}
