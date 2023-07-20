
namespace CloudPillar.Agent.Entities.Twin;

public class ExecuteAction : TwinAction
{
    public ShellType Shell;
    public string Command;

    public ExecuteAction()
    {
        this.ActionName = TwinActionType.ExecuteOnce;
    }
}