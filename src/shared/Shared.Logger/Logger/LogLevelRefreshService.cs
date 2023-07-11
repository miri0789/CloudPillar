using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Shared.Logger
{
    public class LogLevelRefreshService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _refresher;
        private readonly ILoggerHandler _logger;

        public LogLevelRefreshService(IConfiguration configuration, IConfigurationRefresher refresher, ILoggerHandler logger)
        {
            _configuration = configuration;
            _refresher = refresher;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _refresher.TryRefreshAsync();
                
                var defaultLogLevel = _configuration["Logging:LogLevel:Default"];
                var appInsightsLogLevel = _configuration["Logging:LogLevel:AppInsights"];
                var appendersLogLevel = _configuration["Logging:LogLevel:Appenders"];

                _logger.RefreshAppInsightsLogLevel(appInsightsLogLevel != null ? appInsightsLogLevel : 
                                                  appendersLogLevel != null ? appendersLogLevel:
                                                  defaultLogLevel != null ? defaultLogLevel : "Debug");

                _logger.RefreshAppendersLogLevel(appendersLogLevel != null ? appendersLogLevel:
                                                appInsightsLogLevel != null ? appInsightsLogLevel : 
                                                defaultLogLevel != null ? defaultLogLevel : "Debug");

                var interval = _configuration["Logging:LogLevel:RefreshInterval"];

                await Task.Delay(TimeSpan.FromMilliseconds(interval != null ? Convert.ToDouble(interval) : 15000), stoppingToken);  
            }
        }
    }
}