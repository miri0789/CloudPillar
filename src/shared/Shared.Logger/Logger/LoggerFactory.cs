using log4net;
using log4net.Config;
using log4net.Repository;
using System.Reflection;

namespace shared.Logger;

public class LoggerFactory : ILoggerFactory
{
    public ILog CreateLogger(string filename)
    {
        return LogManager.GetLogger(filename);
    }

    public ILoggerRepository createLogRepository(string log4netConfigFile)
    {
        var logRepository = LogManager.GetRepository(Assembly.GetExecutingAssembly());
        if (String.IsNullOrEmpty(log4netConfigFile))
        {
            XmlConfigurator.Configure(logRepository);
        }
        else
        {
            XmlConfigurator.Configure(logRepository, new FileInfo(log4netConfigFile));
        }
        return logRepository;
    }

    public ITelemetryClientWrapper CreateTelemetryClient(string appInsightsKey, string connectionString)
    {

        return new TelemetryClientWrapper(appInsightsKey, connectionString);
        
    }
}