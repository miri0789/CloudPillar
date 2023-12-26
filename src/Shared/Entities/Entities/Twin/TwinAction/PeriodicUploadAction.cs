
namespace Shared.Entities.Twin;

public class PeriodicUploadAction : TwinAction
{
    public string DirName { get; set; }
    public int? Interval { get; set; }
    public FileUploadMethod Method { get; set; } = FileUploadMethod.Stream;
    

    public PeriodicUploadAction()
    {
        Action = TwinActionType.PeriodicUpload;
    }
}
