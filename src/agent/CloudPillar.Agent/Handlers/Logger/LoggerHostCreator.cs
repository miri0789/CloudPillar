namespace CloudPillar.Agent.Handlers.Logger
{
    public static class LoggerHostCreator
    {
        public static WebApplicationBuilder Configure(string applicationName, WebApplicationBuilder? builder = null)
        {
            if (builder == null)
            {
                builder = WebApplication.CreateBuilder();
            }
            builder.Services.AddHttpContextAccessor();
            string? appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig");
            var fileName = "log4net.config";
            var lo4NetPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, fileName); 
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