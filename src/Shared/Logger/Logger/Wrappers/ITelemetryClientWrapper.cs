﻿using Microsoft.ApplicationInsights.DataContracts;

namespace Shared.Logger.Wrappers;

public interface ITelemetryClientWrapper
{
    void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties);
    void TrackException(Exception e, IDictionary<string, string> properties);
    void Flush();
}
