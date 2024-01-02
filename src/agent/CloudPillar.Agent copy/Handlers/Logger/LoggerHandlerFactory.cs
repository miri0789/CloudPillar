using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Appender;
using System.Reflection;

namespace CloudPillar.Agent.Handlers.Logger;

public class LoggerHandlerFactory : ILoggerHandlerFactory
{
    private ILoggerRepository m_repository;

    public ILog CreateLogger(string filename)
    {
        return LogManager.GetLogger(filename);
    }

    public LoggerHandlerFactory()
    {
        m_repository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
    }

    public ILoggerRepository CreateLogRepository(string? log4netConfigFile)
    {
        if (string.IsNullOrEmpty(log4netConfigFile))
        {
            XmlConfigurator.Configure(m_repository);
        }
        else
        {
            XmlConfigurator.Configure(m_repository, new FileInfo(log4netConfigFile));
        }
        return m_repository;
    }

    public IAppender[] GetAppenders()
    {
        return m_repository.GetAppenders();
    }

    public Level? GetLevel(string logLevel)
    {
        return m_repository.LevelMap[logLevel];
    }

    public T? FindAppender<T>() where T : IAppender
    {
        var appenders = GetAppenders();
        return appenders.OfType<T>().FirstOrDefault();
    }
}