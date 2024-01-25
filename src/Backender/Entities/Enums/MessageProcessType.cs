
namespace Backender.Entities.Enums;
public enum MessageProcessType
{
    Retain, 
    ConsumeSuccess, 
    ConsumeErrorRecoverable, 
    ConsumeErrorFatal
}