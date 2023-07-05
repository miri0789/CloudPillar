using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Logger
{
    partial class Program
    {  
        private static IConfigurationRefresher _refresher = null;

        static async Task Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
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

                Logger.Init("test-logger",
                configuration["Logging:AppInsights:InstrumentationKey"], "log4net.config", "Biosense.CloudPillar.Backend",
                configuration["Logging:AppInsights:ConnectionString"] );

                services.AddSingleton<LogLevelRefreshService>(new LogLevelRefreshService(configuration, _refresher));
                services.AddHostedService(provider => provider.GetRequiredService<LogLevelRefreshService>());
            });
    }
}
