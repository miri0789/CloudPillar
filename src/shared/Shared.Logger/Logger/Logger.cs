using System.Reflection;
using System.Web;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.ApplicationInsights.Log4NetAppender;
using Microsoft.Extensions.Hosting;

namespace shared.Logger;

public class Logger : ILogger
{
    private ILog m_logger;

    private ITelemetryClientWrapper m_telemetryClient;

    private string m_applicationName;

    private bool m_hasHttpContext;

    private readonly IHttpContextAccessor m_httpContextAccessor;

    private Level m_appInsightsLogLevel;
    private SeverityLevel m_appInsightsSeverity;

    private Level? m_appendersLogLevel;

    private ApplicationInsightsAppender? m_appInsightsAppender;

    public Logger(ILoggerFactory loggerFactory, string filename,
                          string appInsightsKey = null, string log4netConfigFile = null,
                          string applicationName = null, string connectionString = null) : this(loggerFactory,
                              null, loggerFactory.CreateLogger(filename), appInsightsKey, log4netConfigFile,
                              applicationName, connectionString, false)
    { }

    public Logger(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, ILog logger, string appInsightsKey = null, string log4netConfigFile = null, string applicationName = null, string connectionString = null, bool hasHttpContext = true)
    {
        m_httpContextAccessor = httpContextAccessor;
        m_logger = logger;
        m_hasHttpContext = hasHttpContext;

        ILoggerRepository? repo = null;
        if (!string.IsNullOrWhiteSpace(log4netConfigFile))
        {
            repo = loggerFactory.createLogRepository(log4netConfigFile);
        }

        m_appInsightsAppender = repo?.GetAppenders().OfType<ApplicationInsightsAppender>().FirstOrDefault() as ApplicationInsightsAppender;

        if (!string.IsNullOrWhiteSpace(appInsightsKey))
        {
            m_applicationName = applicationName;

            if (m_appInsightsAppender != null)
            {
                m_appInsightsAppender.InstrumentationKey = appInsightsKey;
                m_appInsightsAppender.ActivateOptions();
            }
            else
            {
                m_telemetryClient = loggerFactory.CreateTelemetryClient(appInsightsKey, connectionString);
            }
        }
        
        AddCustomLogLevel("Debug", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["DEBUG"].Value);
        AddCustomLogLevel("Info", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["INFO"].Value);
        AddCustomLogLevel("Information", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["INFO"].Value);
        AddCustomLogLevel("Warn", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["WARN"].Value);
        AddCustomLogLevel("Warning", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["WARN"].Value);
        AddCustomLogLevel("Error", LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap["ERROR"].Value);

        Log4netConfigurationValidator.ValidateConfiguration(this);
    }

    public IHostBuilder GetLoggerHostBuilder(string[] args)
    {
        LoggerHostBuilder loggerBuilder = new LoggerHostBuilder(this);
        return loggerBuilder.CreateHostBuilder(args);
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
            catch (FormatException e)
            {
                var x = e;
                formattedMessage = string.Join(Environment.NewLine, formattedMessage, "args:", string.Join(", ", args));
            }
        }

        return formattedMessage;

    }
    public void Flush()
    {
        var appenders = LogManager.GetRepository(Assembly.GetExecutingAssembly()).GetAppenders().OfType<BufferingAppenderSkeleton>();

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

    private void TraceLogAppInsightsInternal(string message, SeverityLevel severityLevel, IDictionary<string, string> properties = null)
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

    private void ExceptionLogAppInsightsInternal(Exception e, IDictionary<string, string> properties = null)
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
        Level? level = GetLevel(logLevel);
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
                
        ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);

        Info($"App Insights Log Level changed to {logLevel}"); 

        m_appInsightsLogLevel = level;
        
        // For manually controlling telemetry trace log level severity
        m_appInsightsSeverity = GetSeverityLevel(m_appInsightsLogLevel);
    }

    public void RefreshAppendersLogLevel(string logLevel)
    {
        Level? level = GetLevel(logLevel);
        if(level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }
        if (m_appendersLogLevel == level)
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
                
        m_appendersLogLevel = level;
    }

    private Level? GetLevel(string logLevel)
    {
        return LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap[logLevel];
    }

    public static SeverityLevel GetSeverityLevel(Level logLevel)
    {
        Dictionary<Level, SeverityLevel> levelMap = new Dictionary<Level, SeverityLevel>
        {
            { Level.Trace, SeverityLevel.Verbose },
            { Level.Debug, SeverityLevel.Verbose },
            { Level.Info, SeverityLevel.Information },
            { Level.Warn, SeverityLevel.Warning },
            { Level.Error, SeverityLevel.Error },
            { Level.Critical, SeverityLevel.Critical }
        };

        return levelMap.TryGetValue(logLevel, out SeverityLevel severityLevel)
            ? severityLevel
            : SeverityLevel.Verbose;
    }

    public void AddCustomLogLevel(string logLevel, int value)
    {
        LogManager.GetRepository(Assembly.GetExecutingAssembly()).LevelMap.Add(logLevel, value);
    }
}
