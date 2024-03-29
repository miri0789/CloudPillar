﻿using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Shared.Logger.Wrappers;

public class TelemetryClientWrapper: ITelemetryClientWrapper
{
    TelemetryClient m_telemetryClient;
    public TelemetryClientWrapper(string connectionString)
    {
        var configuration = TelemetryConfiguration.CreateDefault();

        if (!String.IsNullOrEmpty(connectionString))
        {
            configuration.ConnectionString = connectionString;
        }

        m_telemetryClient = new TelemetryClient(configuration);
    }

    public void Flush()
    {
        m_telemetryClient.Flush();
    }

    public void TrackException(Exception e, IDictionary<string, string> properties)
    {
        m_telemetryClient.TrackException(e, properties);
    }

    public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string> properties)
    {
        m_telemetryClient.TrackTrace(message, severityLevel, properties);
    }
}
