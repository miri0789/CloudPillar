using log4net;
using log4net.Repository;
using log4net.Appender;
using log4net.Core;
using Shared.Logger.Wrappers;

namespace Shared.Logger;

public interface ILoggerHandlerFactory
{
    ILog CreateLogger(string filename);

    ILoggerRepository CreateLogRepository(string? log4netConfigFile);

    IAppender[] GetAppenders();

    Level? GetLevel(string logLevel);

    void RaiseConfigurationChanged(EventArgs e);

    ITelemetryClientWrapper CreateTelemetryClient(string connectionString);

    bool IsApplicationInsightsAppenderInRoot();
}