namespace Shared.Entities.Twin;

public enum ResultCode
{
    Done,
    FinishedTransit,
    NotFound,
    StrictModeSize,
    StrictModePattern,
    StrictModeBashPowerShell,
    StrictModeRootNotFound,
    StrictModeMultipleRestrictionSameId,
}