
namespace Shared.Entities.Twin;

public class ExecuteAction : TwinAction
{
    public ShellType Shell;
    public string Command;
    public string OnPause;
    public string OnResume;

    public ExecuteAction()
    {
        this.Action = TwinActionType.ExecuteOnce;
    }
}