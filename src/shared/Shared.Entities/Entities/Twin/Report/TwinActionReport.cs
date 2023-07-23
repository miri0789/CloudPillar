
namespace Shared.Entities.Twin;

public class TwinActionReport
{
    public StatusType? Status { get; set; }
    public int? Progress { get; set; }
    public ResultCode? ResultCode { get; set; }
    public string ResultText { get; set; }
}