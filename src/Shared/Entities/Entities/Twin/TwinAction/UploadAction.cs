
namespace Shared.Entities.Twin;

public class UploadAction : TwinAction
{
    public string FileName { get; set; }
    public FileUploadMethod Method { get; set; } = FileUploadMethod.Stream;
    

    public UploadAction()
    {
        Action = TwinActionType.SingularUpload;
    }
}
