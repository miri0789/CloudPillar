using System.Web;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.ApplicationInsights.Log4NetAppender;
using Shared.Logger.Wrappers;

namespace Shared.Logger;

public class LoggerHandler : ILoggerHandler
{
    private ILog m_logger;

    private ITelemetryClientWrapper? m_telemetryClient;
    
    ILoggerHandlerFactory m_loggerHandlerFactory;

    private string m_applicationName;

    private bool m_hasHttpContext;

    private readonly IHttpContextAccessor? m_httpContextAccessor;

    private Level? m_appInsightsLogLevel;
    
    private SeverityLevel m_appInsightsSeverity;

    private Level? m_appendersLogLevel;

    private ApplicationInsightsAppender? m_appInsightsAppender;

    private Dictionary<Level, SeverityLevel> m_levelMap = new Dictionary<Level, SeverityLevel>
        {
            { Level.Trace, SeverityLevel.Verbose },
            { Level.Debug, SeverityLevel.Verbose },
            { Level.Info, SeverityLevel.Information },
            { Level.Warn, SeverityLevel.Warning },
            { Level.Error, SeverityLevel.Error },
            { Level.Critical, SeverityLevel.Critical }
        };

    public LoggerHandler(ILoggerHandlerFactory loggerFactory, string filename,
                          string? appInsightsKey = null, string? log4netConfigFile = null,
                          string applicationName = "", string? connectionString = null) : this(loggerFactory,
                              null, loggerFactory.CreateLogger(filename), appInsightsKey, log4netConfigFile,
                              applicationName, connectionString, false)
    { }

    public LoggerHandler(ILoggerHandlerFactory loggerFactory, IHttpContextAccessor? httpContextAccessor, ILog logger, string? appInsightsKey = null, string? log4netConfigFile = null, string applicationName = "", string? connectionString = null, bool hasHttpContext = true)
    {
        ArgumentNullException.ThrowIfNull(logger);
        m_logger = logger;

        m_hasHttpContext = hasHttpContext;
        m_httpContextAccessor = httpContextAccessor;

        ArgumentNullException.ThrowIfNull(loggerFactory);
        m_loggerHandlerFactory = loggerFactory;

        m_loggerHandlerFactory.CreateLogRepository(log4netConfigFile);

        m_applicationName = applicationName;
       
        m_appInsightsAppender = FindAppender<ApplicationInsightsAppender>() as ApplicationInsightsAppender; 

        if (m_appInsightsAppender != null)
        {
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                m_appInsightsAppender.InstrumentationKey = appInsightsKey;
                m_appInsightsAppender.ActivateOptions();
            }
            else
            {
                Info("Cannot activate Application Insights: Instrumentation Key is null");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                m_telemetryClient = m_loggerHandlerFactory.CreateTelemetryClient(connectionString);
            }
            else
            {
                Info("Cannot activate Telemetry: Connection String is null");
            }
        }

        RefreshAppInsightsLogLevel(LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD);
        RefreshAppendersLogLevel(LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD);
        
        Log4netConfigurationValidator.ValidateConfiguration(this);
    }

    public T? FindAppender<T>() where T : IAppender
    {
        var appenders = m_loggerHandlerFactory.GetAppenders();
        return appenders.OfType<T>().FirstOrDefault();
    }

    public void Error(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);

        m_logger?.Error(formattedMessage);
        TraceLogAppInsights(formattedMessage, SeverityLevel.Error);
    }

    public void Error(string message, Exception e, params object[] args)
    {
        var error = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Error(error);
        ExceptionLogAppInsights(e);
    }

    public void Warn(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Warn(formattedMessage);
        TraceLogAppInsights(formattedMessage, SeverityLevel.Warning);
    }

    public void Warn(string message, Exception e, params object[] args)
    {
        var warn = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Warn(warn);
        ExceptionLogAppInsights(e);
    }

    public void Info(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Info(formattedMessage);
        TraceLogAppInsights(formattedMessage, SeverityLevel.Information);
    }

    public void Debug(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Debug(formattedMessage);
        TraceLogAppInsights(formattedMessage, SeverityLevel.Verbose);
    }

    private string FormatMsg(string message, params object[] args)
    {
        var formattedMessage = message;

        if (args.Any())
        {
            try
            {
                formattedMessage = string.Format(message, args);
            }
            catch (FormatException)
            {
                formattedMessage = string.Join(Environment.NewLine, formattedMessage, "args:", string.Join(", ", args));
            }
        }

        return formattedMessage;
    }

    public void Flush()
    {
        var appenders = m_loggerHandlerFactory.GetAppenders().OfType<BufferingAppenderSkeleton>();

        foreach (var appender in appenders)
        {
            appender.Flush();
        }

       m_telemetryClient?.Flush();
    }

    private void TraceLogAppInsights(string message, SeverityLevel severityLevel)
    {
        if (m_appInsightsSeverity > severityLevel)
        {
            return;
        }

        if (m_hasHttpContext)
        {
            TraceLogAppInsightsWithSessionParams(message, severityLevel);
        }
        else
        {
            TraceLogAppInsightsInternal(message, severityLevel);
        }
    }

    private void ExceptionLogAppInsights(Exception e)
    {
        if (m_hasHttpContext)
        {
            ExceptionLogAppInsightsWithSessionParams(e);
        }
        else
        {
            ExceptionLogAppInsightsInternal(e);
        }
    }

    private Dictionary<string, string> GetRequestParameters()
    {
        var httpContexProperties = new Dictionary<string, string>();
        if (m_httpContextAccessor?.HttpContext != null)
        {
            var context = m_httpContextAccessor.HttpContext;
            context.Request.Headers.TryGetValue(LoggerConstants.SESSION_USER_NAME, out var userName);
            context.Request.Headers.TryGetValue(LoggerConstants.SESSION_TENANT_ID, out var tenantId);
            context.Request.Query.TryGetValue(LoggerConstants.SESSION_CASE_ID, out var caseId);
            context.Request.Query.TryGetValue(LoggerConstants.SESSION_CORRELATION_ID, out var correlationId);

            httpContexProperties.Add(LoggerConstants.SESSION_USER_NAME, HttpUtility.UrlDecode(userName.ToString()));
            httpContexProperties.Add(LoggerConstants.SESSION_TENANT_ID, tenantId.ToString());
            httpContexProperties.Add(LoggerConstants.SESSION_CASE_ID, caseId.ToString());
            httpContexProperties.Add(LoggerConstants.SESSION_CORRELATION_ID, correlationId.ToString());
        }
        return httpContexProperties;

    }

    private void TraceLogAppInsightsWithSessionParams(string message, SeverityLevel severityLevel)
    {
        var httpContexProperties = GetRequestParameters();

        TraceLogAppInsightsInternal(message, severityLevel, httpContexProperties);
    }

    private void TraceLogAppInsightsInternal(string message, SeverityLevel severityLevel, IDictionary<string, string>? properties = null)
    {
        if (null == properties)
        {
            properties = new Dictionary<string, string>();
        }
        properties.Add(LoggerConstants.APPLICATION_NAME, m_applicationName);
        m_telemetryClient?.TrackTrace(message, severityLevel, properties);
    }

    private void ExceptionLogAppInsightsWithSessionParams(Exception e)
    {

        var httpContexProperties = GetRequestParameters();

        ExceptionLogAppInsightsInternal(e,
           httpContexProperties);
    }

    private void ExceptionLogAppInsightsInternal(Exception e, IDictionary<string, string>? properties = null)
    {
        if (null == properties)
        {
            properties = new Dictionary<string, string>();
        }
        properties.Add(LoggerConstants.APPLICATION_NAME, m_applicationName);
        m_telemetryClient?.TrackException(e, properties);
    }

    public void RefreshAppInsightsLogLevel(string logLevel)
    {
        Level? level = m_loggerHandlerFactory.GetLevel(logLevel);
        if(level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }

        if (m_appInsightsLogLevel == level)
        {
            return;
        }

        if (m_appInsightsAppender != null)
        {
            m_appInsightsAppender.Threshold = level;
            m_appInsightsAppender.ActivateOptions();
        }
                
        m_loggerHandlerFactory.RaiseConfigurationChanged(EventArgs.Empty);

        Info($"App Insights Log Level changed to {logLevel}"); 

        m_appInsightsLogLevel = level;
        
        // For manually controlling telemetry trace log level severity
        m_appInsightsSeverity = GetSeverityLevel(m_appInsightsLogLevel);
    }

    public void RefreshAppendersLogLevel(string logLevel)
    {
        Level? level = m_loggerHandlerFactory.GetLevel(logLevel);
        if(level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }
        if (m_appendersLogLevel == level)
        {
            return;
        }

        var appenders = m_loggerHandlerFactory.GetAppenders().OfType<AppenderSkeleton>()
            .Where(a => !(a is ApplicationInsightsAppender));

        foreach (var appender in appenders)
        {
            appender.Threshold = level;
            m_loggerHandlerFactory.RaiseConfigurationChanged(EventArgs.Empty);
            Info($"Appender {appender.Name} Log Level changed to {logLevel}");
        } 
                
        m_appendersLogLevel = level;
    }

    private SeverityLevel GetSeverityLevel(Level logLevel)
    {
        return m_levelMap.TryGetValue(logLevel, out SeverityLevel severityLevel)
            ? severityLevel
            : SeverityLevel.Verbose;
    }
}
