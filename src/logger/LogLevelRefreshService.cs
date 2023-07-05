using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Logger
{
    public class LogLevelRefreshService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _refresher;

        public LogLevelRefreshService(IConfiguration configuration, IConfigurationRefresher refresher)
        {
            _configuration = configuration;
            _refresher = refresher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _refresher.TryRefreshAsync();
                
                var defaultLogLevel = _configuration["Logging:LogLevel:Default"];
                var appInsightsLogLevel = _configuration["Logging:LogLevel:AppInsights"];
                var appendersLogLevel = _configuration["Logging:LogLevel:Appenders"];

                Logger.RefreshAppInsightsLogLevel(appInsightsLogLevel != null ? appInsightsLogLevel : 
                                                  appendersLogLevel != null ? appendersLogLevel:
                                                  defaultLogLevel != null ? defaultLogLevel : "Debug");

                Logger.RefreshAppendersLogLevel(appendersLogLevel != null ? appendersLogLevel:
                                                appInsightsLogLevel != null ? appInsightsLogLevel : 
                                                defaultLogLevel != null ? defaultLogLevel : "Debug");

                var interval = _configuration["Logging:LogLevel:RefreshInterval"];

                Log4netConfigurationValidator.ValidateConfiguration();

                await Task.Delay(TimeSpan.FromMilliseconds(interval != null ? Convert.ToDouble(interval) : 15000), stoppingToken);  
            }
        }
    }
}