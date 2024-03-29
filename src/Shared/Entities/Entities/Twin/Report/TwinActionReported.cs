﻿
namespace Shared.Entities.Twin;

public class TwinActionReported
{
    public StatusType? Status { get; set; }
    public float? Progress { get; set; }
    public string? ResultCode { get; set; }
    public string? ResultText { get; set; }
    public string? CheckSum { get; set; }
    public string? CompletedRanges { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, TwinActionReported>? PeriodicReported { get; set; }
}