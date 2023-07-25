
namespace Shared.Entities.Twin;

public class TwinActionReport
{
    public StatusType? Status { get; set; }
    public float? Progress { get; set; }
    public string ResultCode { get; set; }
    public string ResultText { get; set; }
}