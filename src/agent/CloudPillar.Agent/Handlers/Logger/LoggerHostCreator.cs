using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using log4net.Repository;
using log4net.Appender;
using log4net;

namespace CloudPillar.Agent.Handlers.Logger
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
            //string workingDir = args != null && args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            //builder.Configuration.SetBasePath(workingDir).AddJsonFile("appsettings.json");

            var lo4NetPath = "log4net.config";
            if (!File.Exists(lo4NetPath))
            {
                throw new Exception("no log4net config file");
            }
            log4net.Config.XmlConfigurator.Configure(new FileInfo(lo4NetPath));

            builder.Services.AddSingleton<ILoggerHandler>(sp =>
               {
                   var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                   var logggerFactory = new LoggerHandlerFactory();
                   var logger = new LoggerHandler(logggerFactory, builder.Configuration, httpContextAccessor, logggerFactory.CreateLogger(applicationName),
                        lo4NetPath,
                       applicationName, true);
                   return logger;
               });

            return builder;
        }
    }
}