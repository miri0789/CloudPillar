using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using log4net.Repository;
using log4net.Appender;
using log4net;
using Microsoft.ApplicationInsights.Log4NetAppender;

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
            builder.Services.AddHttpContextAccessor();
            IConfigurationRefresher? refresher = null;
            string? appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig");
            var fileName = "log4net.config";
            var lo4NetPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, fileName); 
            if (!File.Exists(lo4NetPath))
            {
                throw new Exception("no log4net config file");
            }
            log4net.Config.XmlConfigurator.Configure(new FileInfo(lo4NetPath));
            var repository = LogManager.GetRepository();
            bool isAppenderDefined = repository.IsAppenderExists<ApplicationInsightsAppender>();

            if (isAppenderDefined)
            {
                if (string.IsNullOrEmpty(appConfigConnectionString))
                {
                    throw new ArgumentNullException(nameof(appConfigConnectionString));
                }
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
            }
            builder.Services.AddSingleton<ILoggerHandler>(sp =>
               {
                   var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                   var logggerFactory = new LoggerHandlerFactory();
                   var logger = new LoggerHandler(logggerFactory, builder.Configuration, httpContextAccessor, logggerFactory.CreateLogger(applicationName),
                       lo4NetPath,
                       applicationName, true);
                   return logger;
               });

            if (isAppenderDefined)
            {
                builder.Services.AddSingleton(provider =>
                    { return new LogLevelRefreshService(builder.Configuration, refresher, provider.GetRequiredService<ILoggerHandler>()); });
                builder.Services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());
            }

            return builder;
        }
    }
}