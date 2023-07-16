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
                        refreshOptions.Register("Log4Net:LogLevel:Default", refreshAll: true)
                                      .Register("Log4Net:LogLevel:AppInsights", refreshAll: true)
                                      .Register("Log4Net:LogLevel:Appenders", refreshAll: true)
                                      .Register("Logging:LogLevel:RefreshInterval", refreshAll: true)
                                      .Register("Logging:AppInsights:InstrumentationKey", refreshAll: false)
                                      .Register("Logging:AppInsights:ConnectionString", refreshAll: false);

                        refresher = options.GetRefresher();
                    }));
            
            builder.Services.AddSingleton<LogLevelRefreshService>(provider => 
                {return new LogLevelRefreshService(builder.Configuration, refresher, provider.GetRequiredService<ILoggerHandler>());});
            builder.Services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());

            return builder;
        }
    }
}