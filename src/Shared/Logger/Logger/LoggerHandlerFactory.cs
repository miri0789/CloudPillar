using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using System.Reflection;
using Shared.Logger.Wrappers;
using Microsoft.ApplicationInsights.Log4NetAppender;

namespace Shared.Logger;

public class LoggerHandlerFactory : ILoggerHandlerFactory
{
    private ILoggerRepository m_repository;

    public ILog CreateLogger(string filename)
    {
        return LogManager.GetLogger(filename);
    }

    public ILoggerRepository CreateLogRepository(string? log4netConfigFile)
    {
        m_repository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
        if (String.IsNullOrEmpty(log4netConfigFile))
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

    public void RaiseConfigurationChanged(EventArgs e)
    {
        ((Hierarchy)m_repository).RaiseConfigurationChanged(e);
    }

    public ITelemetryClientWrapper CreateTelemetryClient(string connectionString)
    {
        return new TelemetryClientWrapper(connectionString);     
    }

    public bool IsApplicationInsightsAppenderInRoot()
    {
        return m_repository.IsAppenderInRoot<ApplicationInsightsAppender>();     
    }
    
    public T? FindAppender<T>() where T : IAppender
    {
        var appenders = GetAppenders();
        return appenders.OfType<T>().FirstOrDefault();
    }
}