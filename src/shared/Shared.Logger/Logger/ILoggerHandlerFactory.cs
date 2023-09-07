using log4net;
using log4net.Repository;
using log4net.Appender;
using log4net.Core;
using Shared.Logger.Wrappers;

namespace Shared.Logger;

public interface ILoggerHandlerFactory
{
    public ILog CreateLogger(string filename);

    public ILoggerRepository CreateLogRepository(string? log4netConfigFile);

    IAppender[] GetAppenders();

    public Level? GetLevel(string logLevel);

    void RaiseConfigurationChanged(EventArgs e);

    public ITelemetryClientWrapper CreateTelemetryClient(string connectionString);
}