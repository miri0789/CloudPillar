
namespace Shared.Entities.Twin;

public class TwinPatch
{
    public TwinAction[] PreTransitConfig { get; set; }
    public TwinAction[] TransitPackage { get; set; }
    public TwinAction[] PreInstallConfig { get; set; }
    public TwinAction[] InstallSteps { get; set; }
    public TwinAction[] PostInstallConfig { get; set; }
}
