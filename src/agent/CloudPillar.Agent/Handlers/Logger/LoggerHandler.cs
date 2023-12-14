using log4net;
using log4net.Appender;
using log4net.Core;

namespace CloudPillar.Agent.Handlers.Logger;

public class LoggerHandler : ILoggerHandler
{
    private ILog _logger;

    private ILoggerHandlerFactory _loggerHandlerFactory;

    private string _applicationName;

    private bool _hasHttpContext;

    private readonly IHttpContextAccessor? _httpContextAccessor;

    private Level? _appendersLogLevel;

    public LoggerHandler(ILoggerHandlerFactory loggerFactory, IConfiguration configuration, IHttpContextAccessor? httpContextAccessor, ILog logger, string? log4netConfigFile = null, string applicationName = "", bool hasHttpContext = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerHandlerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _loggerHandlerFactory.CreateLogRepository(log4netConfigFile);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _loggerHandlerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        ArgumentNullException.ThrowIfNull(configuration);

        _hasHttpContext = hasHttpContext;
        _httpContextAccessor = httpContextAccessor;

        _applicationName = applicationName;

        // Add Console Appender if not configured
        if (_loggerHandlerFactory.FindAppender<ConsoleAppender>() == null)
        {
            var hierarchy = ((log4net.Core.ILoggerWrapper)_logger).Logger.Repository as log4net.Repository.Hierarchy.Hierarchy;
            if (hierarchy != null)
            {
                hierarchy.Root.AddAppender(CreateConsoleAppender());
            }
        }
        Log4netConfigurationValidator.ValidateConfiguration(this);
    }

    private ConsoleAppender CreateConsoleAppender()
    {
        ConsoleAppender appender = new ConsoleAppender();
        appender.Name = "ConsoleAppender";

        log4net.Layout.PatternLayout layout = new log4net.Layout.PatternLayout();
        layout.ConversionPattern = "%date [%-3thread] %-5level - %message%newline";
        layout.ActivateOptions();

        appender.Layout = layout;
        appender.ActivateOptions();

        return appender;
    }

    public void Error(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);

        _logger?.Error(formattedMessage);
    }

    public void Error(string message, Exception e, params object[] args)
    {
        var error = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Error(error);
    }

    public void Warn(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Warn(formattedMessage);
    }

    public void Warn(string message, Exception e, params object[] args)
    {
        var warn = string.Join(Environment.NewLine, FormatMsg(message, args), e);

        Warn(warn);
    }

    public void Info(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Info(formattedMessage);
    }

    public void Debug(string message, params object[] args)
    {
        var formattedMessage = FormatMsg(message, args);
        _logger?.Debug(formattedMessage);
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
        var appenders = _loggerHandlerFactory.GetAppenders().OfType<BufferingAppenderSkeleton>();

        foreach (var appender in appenders)
        {
            appender.Flush();
        }
    }
}
