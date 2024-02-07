namespace Backender.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string ServiceBusConnectionString { get; }
    string[] ServiceBusUrls { get; }
    int ParallelCount { get; }
    int MaxLockDurationSeconds { get; }
    string SvcBackendUrl { get; }
    int HigherPriorityGraceMS { get; }
    int NoMessagesDelayMS { get; }
    int RequestTimeoutSeconds { get; }
    string CompletionTopic { get; }
    string CompletionUrlBase { get; }
}