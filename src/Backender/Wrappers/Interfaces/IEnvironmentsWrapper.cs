﻿namespace Backender.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string serviceBusConnectionString { get; }
    string[] serviceBusUrls { get; }
    int parallelCount { get; }
    int maxLockDurationSeconds { get; }
    string svcBackendUrl { get; }
    int higherPriorityGraceMS { get; }
    int noMessagesDelayMS { get; }
    int requestTimeoutSeconds { get; }
    string completionTopic { get; }
    string completionUrlBase { get; }
}