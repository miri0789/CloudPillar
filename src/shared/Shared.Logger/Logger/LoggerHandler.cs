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
    private ILog _logger;

    private ITelemetryClientWrapper? _telemetryClient;

    private ILoggerRepository _repository;

    private string _applicationName;

    private bool _hasHttpContext;

    private readonly IHttpContextAccessor? _httpContextAccessor;

    private Level? _appInsightsLogLevel;

    private SeverityLevel _appInsightsSeverity;

    private Level? _appendersLogLevel;

    private ApplicationInsightsAppender? _appInsightsAppender;

    private bool _useAppInsight = false;

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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _repository = loggerFactory.CreateLogRepository(log4netConfigFile);
        ArgumentNullException.ThrowIfNull(configuration);

        _hasHttpContext = hasHttpContext;
        _httpContextAccessor = httpContextAccessor;

        _applicationName = applicationName;


        _appInsightsAppender = Log4netExtentions.GetAppender<ApplicationInsightsAppender>();

        if (_appInsightsAppender != null && Log4netExtentions.IsAppenderInRoot<ApplicationInsightsAppender>(_repository))
        {

            var appInsightsKey = configuration[LoggerConstants.APPINSIGHTS_INSTRUMENTATION_KEY_CONFIG];
            if (!string.IsNullOrWhiteSpace(appInsightsKey))
            {
                _appInsightsAppender.InstrumentationKey = appInsightsKey;
                _appInsightsAppender.ActivateOptions();
                _useAppInsight = true;
            }
            else
            {
                Info("Cannot activate Application Insights: Instrumentation Key is null");
            }
            var connectionString = configuration[LoggerConstants.APPINSIGHTS_CONNECTION_STRING_CONFIG];
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                _telemetryClient = loggerFactory.CreateTelemetryClient(connectionString);
                _useAppInsight = true;
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

        _logger?.Error(formattedMessage);
        if (_useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Error);
    }

    public void Error(string message, Exception e, params object[] args)
    {
        var error = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Error(error);
        if (_useAppInsight) ExceptionLogAppInsights(e);
    }

    public void Warn(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Warn(formattedMessage);
        if (_useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Warning);
    }

    public void Warn(string message, Exception e, params object[] args)
    {
        var warn = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Warn(warn);
        if (_useAppInsight) ExceptionLogAppInsights(e);
    }

    public void Info(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Info(formattedMessage);
        if (_useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Information);
    }

    public void Debug(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Debug(formattedMessage);
        if (_useAppInsight) TraceLogAppInsights(formattedMessage, SeverityLevel.Verbose);
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
        var appenders = _repository.GetAppenders().OfType<BufferingAppenderSkeleton>();

        foreach (var appender in appenders)
        {
            appender.Flush();
        }

        _telemetryClient?.Flush();
    }

    private void TraceLogAppInsights(string message, SeverityLevel severityLevel)
    {
        if (_appInsightsSeverity > severityLevel)
        {
            return;
        }

        if (_hasHttpContext)
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
        if (_hasHttpContext)
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
        if (_httpContextAccessor?.HttpContext != null)
        {
            var context = _httpContextAccessor.HttpContext;
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
        properties.Add(LoggerConstants.APPLICATION_NAME, _applicationName);
        _telemetryClient?.TrackTrace(message, severityLevel, properties);
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
        properties.Add(LoggerConstants.APPLICATION_NAME, _applicationName);
        _telemetryClient?.TrackException(e, properties);
    }

    public void RefreshAppInsightsLogLevel(string logLevel)
    {
        Level? level = GetLevel(logLevel);
        if (level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }

        if (_appInsightsLogLevel == level)
        {
            return;
        }

        if (_appInsightsAppender != null)
        {
            _appInsightsAppender.Threshold = level;
            _appInsightsAppender.ActivateOptions();
        }

        ((Hierarchy)_repository).RaiseConfigurationChanged(EventArgs.Empty);

        Info($"App Insights Log Level changed to {logLevel}");

        _appInsightsLogLevel = level;

        // For manually controlling telemetry trace log level severity
        _appInsightsSeverity = GetSeverityLevel(_appInsightsLogLevel);
    }

    public void RefreshAppendersLogLevel(string logLevel, bool init)
    {
        Level? level = GetLevel(logLevel);
        if (level == null)
        {
            Warn($"Trying to set invalid log level: {logLevel}");
            return;
        }
        if (_appendersLogLevel == level)
        {
            return;
        }

        var appenders = _repository.GetAppenders().OfType<AppenderSkeleton>()
            .Where(a => !(a is ApplicationInsightsAppender));

        foreach (var appender in appenders)
        {
            if (init && appender.Threshold == null)
            {
                appender.Threshold = level;
                ((Hierarchy)_repository).RaiseConfigurationChanged(EventArgs.Empty);
                Info($"Appender {appender.Name} Log Level changed to {logLevel}");
            }
        }

        _appendersLogLevel = level;
    }

    private Level? GetLevel(string logLevel)
    {
        return _repository.LevelMap[logLevel];
    }

    private SeverityLevel GetSeverityLevel(Level logLevel)
    {
        return m_levelMap.TryGetValue(logLevel, out SeverityLevel severityLevel)
            ? severityLevel
            : SeverityLevel.Verbose;
    }
}
