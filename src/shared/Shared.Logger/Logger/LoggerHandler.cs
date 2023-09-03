using System.Web;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.ApplicationInsights.Log4NetAppender;
using Shared.Logger.Wrappers;
using Microsoft.Extensions.Configuration;

namespace Shared.Logger;

public class LoggerHandler : ILoggerHandler
{
    private ILog m_logger;

    private ITelemetryClientWrapper? m_telemetryClient;

    private ILoggerRepository m_repository;

    private string m_applicationName;

    private bool m_hasHttpContext;

    private readonly IHttpContextAccessor? m_httpContextAccessor;

    private Level? m_appInsightsLogLevel;

    private SeverityLevel m_appInsightsSeverity;

    private Level? m_appendersLogLevel;

    private ApplicationInsightsAppender? m_appInsightsAppender;

    private bool useAppInsight = false;

    private Dictionary<Level, SeverityLevel> m_levelMap = new Dictionary<Level, SeverityLevel>
        {
            { Level.Trace, SeverityLevel.Verbose },
            { Level.Debug, SeverityLevel.Verbose },
            { Level.Info, SeverityLevel.Information },
            { Level.Warn, SeverityLevel.Warning },
            { Level.Error, SeverityLevel.Error },
            { Level.Critical, SeverityLevel.Critical }
        };


    public LoggerHandler(ILoggerHandlerFactory loggerFactory, IConfiguration configuration, IHttpContextAccessor? httpContextAccessor, ILog logger, string? log4netConfigFile = null, string applicationName = "", bool hasHttpContext = true)
    {
        m_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        m_repository = loggerFactory.CreateLogRepository(log4netConfigFile);
        ArgumentNullException.ThrowIfNull(configuration);

        m_hasHttpContext = hasHttpContext;
        m_httpContextAccessor = httpContextAccessor;

        m_applicationName = applicationName;


        m_appInsightsAppender = m_repository.GetAppenders().OfType<ApplicationInsightsAppender>().FirstOrDefault();

        if (m_appInsightsAppender != null && Log4netExtentions.IsAppenderInRoot<ApplicationInsightsAppender>(m_repository))
        {

            var appInsightsKey = configuration[LoggerConstants.APPINSIGHTS_INSTRUMENTATION_KEY_CONFIG];
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                m_appInsightsAppender.InstrumentationKey = appInsightsKey;
                m_appInsightsAppender.ActivateOptions();
                useAppInsight = true;
            }
            else
            {
                Info("Cannot activate Application Insights: Instrumentation Key is null");
            }
            var connectionString = configuration[LoggerConstants.APPINSIGHTS_CONNECTION_STRING_CONFIG];
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                m_telemetryClient = loggerFactory.CreateTelemetryClient(connectionString);
                useAppInsight = true;
            }
            else
            {
                Info("Cannot activate Telemetry: Connection String is null");
            }
            RefreshAppInsightsLogLevel(LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD);
        }


        RefreshAppendersLogLevel(LoggerConstants.LOG_LEVEL_DEFAULT_THRESHOLD, true);

        Log4netConfigurationValidator.ValidateConfiguration(this);
    }

    public void Error(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);

        m_logger?.Error(formattedMessage);
        if (useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Error);
    }

    public void Error(string message, Exception e, params object[] args)
    {
        var error = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Error(error);
        if (useAppInsight) ExceptionLogAppInsights(e);
    }

    public void Warn(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Warn(formattedMessage);
        if (useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Warning);
    }

    public void Warn(string message, Exception e, params object[] args)
    {
        var warn = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Warn(warn);
        if (useAppInsight) ExceptionLogAppInsights(e);
    }

    public void Info(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Info(formattedMessage);
        if (useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Information);
    }

    public void Debug(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        m_logger?.Debug(formattedMessage);
       if (useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Verbose);
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
        var appenders = m_repository.GetAppenders().OfType<BufferingAppenderSkeleton>();

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
        Level? level = GetLevel(logLevel);
        if (level == null)
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

        ((Hierarchy)m_repository).RaiseConfigurationChanged(EventArgs.Empty);

        Info($"App Insights Log Level changed to {logLevel}");

        m_appInsightsLogLevel = level;

        // For manually controlling telemetry trace log level severity
        m_appInsightsSeverity = GetSeverityLevel(m_appInsightsLogLevel);
    }

    public void RefreshAppendersLogLevel(string logLevel, bool init)
    {
        Level? level = GetLevel(logLevel);
        if (level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }
        if (m_appendersLogLevel == level)
        {
            return;
        }

        var appenders = m_repository.GetAppenders().OfType<AppenderSkeleton>()
            .Where(a => !(a is ApplicationInsightsAppender));

        foreach (var appender in appenders)
        {
            if (init && appender.Threshold == null)
            {
                appender.Threshold = level;
                ((Hierarchy)m_repository).RaiseConfigurationChanged(EventArgs.Empty);
                Info($"Appender {appender.Name} Log Level changed to {logLevel}");
            }
        }

        m_appendersLogLevel = level;
    }

    private Level? GetLevel(string logLevel)
    {
        return m_repository.LevelMap[logLevel];
    }

    private SeverityLevel GetSeverityLevel(Level logLevel)
    {
        return m_levelMap.TryGetValue(logLevel, out SeverityLevel severityLevel)
            ? severityLevel
            : SeverityLevel.Verbose;
    }
}
