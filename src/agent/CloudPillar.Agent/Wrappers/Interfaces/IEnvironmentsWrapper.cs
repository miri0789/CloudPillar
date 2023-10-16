﻿namespace CloudPillar.Agent.Wrappers;

public interface IEnvironmentsWrapper
{
    string deviceConnectionString { get; }
    string transportType { get; }
    string periodicUploadInterval { get; }
    string dpsScopeId { get; }
    string globalDeviceEndpoint { get; }
    int certificateExpiredDays { get; }
    string groupEnrollmentName { get; }
    string groupEnrollmentPrimaryKey { get; }
}
