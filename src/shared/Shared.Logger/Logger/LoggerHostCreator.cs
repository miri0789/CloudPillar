using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.Logger
{
    public static class LoggerHostCreator
    {
        public static WebApplicationBuilder Configure(string applicationName, WebApplicationBuilder? builder = null, string[]? args = null)
        {
            if (builder == null)
            {
                builder = WebApplication.CreateBuilder(args);
            }

            IConfigurationRefresher? refresher = null;
            string? appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig");
            string? appInsightsInstrumentationKey = null;
            string? appInsightsConnectionString = null;
            builder.Configuration.AddAzureAppConfiguration(options =>
                    options.Connect(appConfigConnectionString)
                    .Select("Log4Net", LabelFilter.Null)
                    .ConfigureRefresh(refreshOptions =>
                    {
                        refreshOptions.Register(LoggerConstants.LOG_LEVEL_DEFAULT_CONFIG, refreshAll: true)
                                      .Register(LoggerConstants.LOG_LEVEL_APPINSIGHTS_CONFIG, refreshAll: true)
                                      .Register(LoggerConstants.LOG_LEVEL_APPENDERS_CONFIG, refreshAll: true)
                                      .Register(LoggerConstants.LOG_LEVEL_INTERVAL_CONFIG, refreshAll: true)
                                      .Register(LoggerConstants.APPINSIGHTS_INSTRUMENTATION_KEY_CONFIG, refreshAll: false)
                                      .Register(LoggerConstants.APPINSIGHTS_CONNECTION_STRING_CONFIG, refreshAll: false);

                        refresher = options.GetRefresher();
                        refresher.TryRefreshAsync();
                    }));
            appInsightsInstrumentationKey = builder.Configuration[LoggerConstants.APPINSIGHTS_INSTRUMENTATION_KEY_CONFIG];
            appInsightsConnectionString = builder.Configuration[LoggerConstants.APPINSIGHTS_CONNECTION_STRING_CONFIG];

            // Create Logger
            builder.Services.AddHttpContextAccessor();

            var applicationInsightsSection = builder.Configuration.GetSection("ApplicationInsights");
            builder.Services.AddSingleton<ILoggerHandler>(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var logggerFactory = new LoggerHandlerFactory();
                var logger = new LoggerHandler(logggerFactory, httpContextAccessor, logggerFactory.CreateLogger(applicationName),
                    appInsightsInstrumentationKey, "log4net.config",
                    applicationName, appInsightsConnectionString, true);
                return logger;
            });

            
            builder.Services.AddSingleton<LogLevelRefreshService>(provider => 
                {return new LogLevelRefreshService(builder.Configuration, refresher, provider.GetRequiredService<ILoggerHandler>());});
            builder.Services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());

            return builder;
        }
    }
}