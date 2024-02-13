
namespace Backender.Entities.Enums;
public enum CompletionCode
{
    Retain = -1,
    ConsumeSuccess = 0,
    ConsumeErrorRecoverable = 1,
    ConsumeErrorFatal = 10
}