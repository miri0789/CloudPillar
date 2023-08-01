
namespace Shared.Entities.Twin;

public class UploadAction : TwinAction
{
    public string FileName { get; set; }
    public int Interval { get; set; }
    public Boolean enabled { get; set; }
}
