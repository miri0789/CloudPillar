using log4net;
using log4net.Config;
using log4net.Repository;
using System.Reflection;
using Shared.Logger.Wrappers;

namespace Shared.Logger;

public class LoggerFactoryHandler : ILoggerHandlerFactory
{
    public ILog CreateLogger(string filename)
    {
        return LogManager.GetLogger(filename);
    }

    public ILoggerRepository CreateLogRepository(string? log4netConfigFile)
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

    public ITelemetryClientWrapper CreateTelemetryClient(string connectionString)
    {
        return new TelemetryClientWrapper(connectionString);     
    }
}