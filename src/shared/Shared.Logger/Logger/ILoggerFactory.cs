using log4net;
using log4net.Repository;
using Microsoft.Extensions.Hosting;

namespace shared.Logger;

public interface ILoggerFactory
{
    public ILog CreateLogger(string filename);

    public ILoggerRepository createLogRepository(string log4netConfigFile);

    public ITelemetryClientWrapper CreateTelemetryClient(string appInsightsKey, string connectionString);
}