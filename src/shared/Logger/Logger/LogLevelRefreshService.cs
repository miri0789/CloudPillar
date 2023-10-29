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
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _refresher.TryRefreshAsync();

                LogLevelOptions options = new LogLevelOptions(_configuration[LoggerConstants.LOG_LEVEL_APPINSIGHTS_CONFIG],
                                                              _configuration[LoggerConstants.LOG_LEVEL_APPENDERS_CONFIG],
                                                              _configuration[LoggerConstants.LOG_LEVEL_DEFAULT_CONFIG]);

                var appInsightsLevelRefresh = options.AppInsights ?? options.Appenders ?? options.Default ?? LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD;
                var appendersLevelRefresh = options.Appenders ?? options.AppInsights ?? options.Default ?? LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD;

                _logger.RefreshAppInsightsLogLevel(appInsightsLevelRefresh);
                _logger.RefreshAppendersLogLevel(appendersLevelRefresh, false);

                var intervalStr = _configuration[LoggerConstants.LOG_LEVEL_INTERVAL_CONFIG];
                double interval = Double.TryParse(intervalStr, out interval) ? interval : 15000;

                await Task.Delay(TimeSpan.FromMilliseconds(interval), stoppingToken);
            }
        }
    }

    public class LogLevelOptions
    {
        public LogLevelOptions(string? appInsightsLevel, string? appendersLevel, string? defaultLevel)
        {
            AppInsights = appInsightsLevel;
            Appenders = appendersLevel;
            Default = defaultLevel;
        }

        public string? AppInsights { get; set; }
        public string? Appenders { get; set; }
        public string? Default { get; set; }
    }
}