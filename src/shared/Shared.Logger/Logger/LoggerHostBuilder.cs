using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Logger
{
    public class LoggerHostBuilder
    {
        private ILoggerHandler _logger;
        private IConfigurationRefresher? _refresher;

        public LoggerHostBuilder(ILoggerHandler logger)
        {
            _logger = logger;
            _refresher = null;
        }

        public IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            { 
                var settings = config.Build();
                config.AddAzureAppConfiguration(options =>
                    options.Connect(settings["ConnectionStrings:AppConfig"])
                    .Select("Logging", LabelFilter.Null)
                    .ConfigureRefresh(refreshOptions =>
                    {
                        refreshOptions.Register("Logging:LogLevel:Default", refreshAll: true)
                                      .Register("Logging:LogLevel:AppInsights", refreshAll: true)
                                      .Register("Logging:LogLevel:Appenders", refreshAll: true)
                                      .Register("Logging:LogLevel:RefreshInterval", refreshAll: true)
                                      .Register("Logging:AppInsights:InstrumentationKey", refreshAll: false)
                                      .Register("Logging:AppInsights:ConnectionString", refreshAll: false);

                        _refresher = options.GetRefresher();
                    }));
            })
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;
                services.Configure<IConfiguration>(configuration);
                services.AddSingleton<LogLevelRefreshService>(new LogLevelRefreshService(configuration, _refresher, _logger));
                services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());
            });
    }
}