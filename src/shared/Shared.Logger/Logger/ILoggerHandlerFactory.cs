using log4net;
using log4net.Repository;
using Shared.Logger.Wrappers;

namespace Shared.Logger;

public interface ILoggerHandlerFactory
{
    public ILog CreateLogger(string filename);

    public ILoggerRepository CreateLogRepository(string log4netConfigFile);

    public ITelemetryClientWrapper CreateTelemetryClient(string appInsightsKey, string connectionString);
}