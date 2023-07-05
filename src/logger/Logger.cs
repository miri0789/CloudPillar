using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.ApplicationInsights.Log4NetAppender;

namespace Logger
{
    public static class Logger
    {
        private static ILog s_logger;

        private static TelemetryClient s_telemetryClient;

        private static string s_applicationName;

        private static bool s_hasHttpContext;

        private static string s_correlationId;

        private static IHttpContextAccessor s_httpContextAccessor;

        private static string s_log4netConfigFile;

        private static Level s_appInsightsLogLevel;

        private static Level s_appendersLogLevel;

        private static ApplicationInsightsAppender? s_appInsightsAppender;

        public static void Init(string filename, string appInsightsKey = "", string log4netConfigFile = "", string applicationName = "", string connectionString = "", IHttpContextAccessor httpCtx = null)
        {
            Init(LogManager.GetLogger(filename), appInsightsKey, log4netConfigFile, applicationName, true, connectionString, httpCtx);
        }

        public static void Init(ILog logger, string appInsightsKey = "", string log4netConfigFile = "", string applicationName = "", bool hasHttpContext = true, string connectionString = "", IHttpContextAccessor httpCtx = null)
        {
            s_logger = logger;
            s_hasHttpContext = hasHttpContext;
            s_httpContextAccessor = httpCtx;
            s_log4netConfigFile = log4netConfigFile;

            var logRepository = LogManager.GetRepository(Assembly.GetExecutingAssembly());

            if (String.IsNullOrEmpty(s_log4netConfigFile))
            {
                XmlConfigurator.Configure(logRepository);
            }
            else
            {
                XmlConfigurator.Configure(logRepository, new FileInfo(s_log4netConfigFile));
            }

            s_appInsightsAppender = LogManager.GetRepository().GetAppenders().OfType<ApplicationInsightsAppender>().FirstOrDefault() as ApplicationInsightsAppender;

            if (!String.IsNullOrEmpty(appInsightsKey))
            {
                var configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = appInsightsKey;
                
                s_appInsightsAppender.InstrumentationKey = appInsightsKey;
                s_appInsightsAppender.ActivateOptions();

                if (!String.IsNullOrEmpty(connectionString))
                {
                    configuration.ConnectionString = connectionString;
                } 

                s_telemetryClient = new TelemetryClient(configuration);   
            }
            
            AddCustomLogLevel("Debug", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["DEBUG"].Value);
            AddCustomLogLevel("Info", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["INFO"].Value);
            AddCustomLogLevel("Information", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["INFO"].Value);
            AddCustomLogLevel("Warning", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["WARN"].Value);
            AddCustomLogLevel("Warn", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["WARN"].Value);
            AddCustomLogLevel("Error", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["ERROR"].Value);
        }

        public static void InitCorrelationId(string correlationId)
        {
            s_correlationId = correlationId;
        }

        private static void TraceLogAppInsights(string message, SeverityLevel severityLevel)
        {
            if (s_hasHttpContext)
            {
                TraceLogAppInsightsWithSessionParams(message, severityLevel);
            }
            else
            {
                TraceLogAppInsightsWithoutSessionParams(message, severityLevel);
            }
        }

        private static void ExceptionLogAppInsights(Exception e)
        {
            if (s_hasHttpContext)
            {
                ExceptionLogAppInsightsWithSessionParams(e);
            }
            else
            {
                ExceptionLogAppInsightsWithoutSessionParams(e);
            }
        }

        private static void TraceLogAppInsightsWithSessionParams(string message, SeverityLevel severityLevel)
        {
            string userName = null, studyUID = null, tenantId = null;

            if (s_httpContextAccessor != null && s_httpContextAccessor.HttpContext != null)
            {
                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_USER_NAME, out StringValues name) && name.Any())
                {
                    userName = name.First();
                }

                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_STUDY_UID, out StringValues study) && study.Any())
                {
                    studyUID = study.First();
                }

                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_TENANT_ID, out StringValues tenant) && tenant.Any())
                {
                    tenantId = tenant.First();
                }
            }

            s_telemetryClient?.TrackTrace(message, severityLevel,
               new Dictionary<string, string> { { Constants.SESSION_USER_NAME, userName }, { Constants.APPLICATION_NAME, s_applicationName }, { Constants.SESSION_STUDY_UID, studyUID }, { Constants.SESSION_TENANT_ID, tenantId }, { Constants.CORRELATION_ID, s_correlationId } });
        }

        private static void TraceLogAppInsightsWithoutSessionParams(string message, SeverityLevel severityLevel)
        {
            s_telemetryClient?.TrackTrace(message, severityLevel,
               new Dictionary<string, string> { { Constants.APPLICATION_NAME, s_applicationName }, { Constants.CORRELATION_ID, s_correlationId } });
        }

        private static void ExceptionLogAppInsightsWithSessionParams(Exception e)
        {
            string userName = null, studyUID = null, tenantId = null;

            if (s_httpContextAccessor != null && s_httpContextAccessor.HttpContext != null)
            {
                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_USER_NAME, out StringValues name) && name.Any())
                {
                    userName = name.First();
                }

                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_STUDY_UID, out StringValues study) && study.Any())
                {
                    studyUID = study.First();
                }

                if (s_httpContextAccessor.HttpContext.Request.Headers.TryGetValue(Constants.SESSION_TENANT_ID, out StringValues tenant) && tenant.Any())
                {
                    tenantId = tenant.First();
                }
            }

            s_telemetryClient?.TrackException(e,
               new Dictionary<string, string> { { Constants.SESSION_USER_NAME, userName }, { Constants.APPLICATION_NAME, s_applicationName }, { Constants.SESSION_STUDY_UID, studyUID }, { Constants.SESSION_TENANT_ID, tenantId } });
        }

        private static void ExceptionLogAppInsightsWithoutSessionParams(Exception e)
        {
            s_telemetryClient?.TrackException(e,
              new Dictionary<string, string> { { Constants.APPLICATION_NAME, s_applicationName } });
        }

        public static void Error(string message, params object[] args)
        {
            s_logger?.ErrorFormat(message, args);
            TraceLogAppInsights(String.Format(message, args), SeverityLevel.Error);
        }

        public static void Error(string message, Exception e, params object[] args)
        {
            var error = String.Join(Environment.NewLine, String.Format(message, args), e);

            s_logger?.Error(error);
            TraceLogAppInsights(error, SeverityLevel.Error);
            ExceptionLogAppInsights(e);
        }

        public static void Warn(string message, params object[] args)
        {
            s_logger?.WarnFormat(message, args);
            TraceLogAppInsights(String.Format(message, args), SeverityLevel.Warning);
        }

        public static void Info(string message, params object[] args)
        {
            s_logger?.InfoFormat(message, args);
            TraceLogAppInsights(String.Format(message, args), SeverityLevel.Information);
        }

        public static void Debug(string message, params object[] args)
        {
            s_logger?.DebugFormat(message, args);
            TraceLogAppInsights(String.Format(message, args), SeverityLevel.Verbose);
        }

        public static void Flush()
        {
            var appenders = LogManager.GetRepository(Assembly.GetExecutingAssembly()).GetAppenders().OfType<BufferingAppenderSkeleton>();

            foreach (var appender in appenders)
            {
                appender.Flush();
            }

            s_telemetryClient?.Flush();
        }

        public static void SetRootLogLevel(string logLevel)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            var level = hierarchy.LevelMap[logLevel];
            if (level != null)
            {
                hierarchy.Root.Level = level;
                hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
                Info($"Log Level changed to {level}");  
            }
            
        }
        
        public static void AddCustomLogLevel(string logLevel, int value)
        {
            LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap.Add(logLevel, value);
        }

        public static void RefreshAppInsightsLogLevel(string logLevel)
        {
            Level? level = GetLevel(logLevel);
            if(level == null)
            {
                Warn($"Trying to set invalid log level: {logLevel}");
                return;
            }

            if (s_appInsightsLogLevel == level)
            {
                return;
            }

            if (s_appInsightsAppender != null)
            {
                s_appInsightsAppender.Threshold = level;
                s_appInsightsAppender.ActivateOptions();
            }
                
            ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);

            Info($"App Insights Log Level changed to {logLevel}"); 

            s_appInsightsLogLevel = level;
        }

        public static void RefreshAppendersLogLevel(string logLevel)
        {
            Level? level = GetLevel(logLevel);
            if(level == null)
            {
                Warn($"Trying to set invalid log level: {logLevel}");
                return;
            }

            if (s_appendersLogLevel == level)
            {
                return;
            }

            var appenders = LogManager.GetRepository(Assembly.GetExecutingAssembly()).GetAppenders().OfType<AppenderSkeleton>()
                .Where(a => !(a is ApplicationInsightsAppender));

            foreach (var appender in appenders)
            {
                appender.Threshold = level;
                ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                Info($"Appender {appender.Name} Log Level changed to {logLevel}");
            } 
                
            s_appendersLogLevel = level;
        }

        private static Level? GetLevel(string logLevel)
        {
            return LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap[logLevel];
        }
    }
}
