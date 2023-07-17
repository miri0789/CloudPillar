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

            // Create Logger
            builder.Services.AddHttpContextAccessor();

            var applicationInsightsSection = builder.Configuration.GetSection("ApplicationInsights");
            builder.Services.AddSingleton<ILoggerHandler>(sp =>
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var logggerFactory = new LoggerHandlerFactory();
                var logger = new LoggerHandler(logggerFactory, httpContextAccessor, logggerFactory.CreateLogger(applicationName),
                    applicationInsightsSection.GetValue<string>("InstrumentationKey"), "log4net.config",
                    applicationName, applicationInsightsSection.GetValue<string>("ConnectionString"), true);
                return logger;
            });

            IConfigurationRefresher? refresher = null;
            string? connectionString = builder.Configuration.GetConnectionString("AppConfig");
            builder.Configuration.AddAzureAppConfiguration(options =>
                    options.Connect(connectionString)
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
                    }));
            
            builder.Services.AddSingleton<LogLevelRefreshService>(provider => 
                {return new LogLevelRefreshService(builder.Configuration, refresher, provider.GetRequiredService<ILoggerHandler>());});
            builder.Services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());

            return builder;
        }
    }
}