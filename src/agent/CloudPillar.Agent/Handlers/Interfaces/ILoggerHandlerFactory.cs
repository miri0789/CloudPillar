using log4net;
using log4net.Repository;
using log4net.Appender;
using log4net.Core;

namespace CloudPillar.Agent.Handlers.Logger;

public interface ILoggerHandlerFactory
{
    ILoggerRepository CreateLogRepository(string? log4netConfigFile);
    ILog CreateLogger(string filename);

    IAppender[] GetAppenders();

    Level? GetLevel(string logLevel);

    T? FindAppender<T>() where T : IAppender;
}