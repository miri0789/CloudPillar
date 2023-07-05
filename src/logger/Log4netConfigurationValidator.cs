using log4net;
using log4net.Core;
using log4net.Layout;
using log4net.Appender;
using Microsoft.ApplicationInsights.Log4NetAppender;

namespace Logger
{
    public static class Log4netConfigurationValidator
    {
        public static void ValidateConfiguration()
        {
            var appenders = LogManager.GetRepository().GetAppenders();
            
            foreach (var appender in appenders)
            {
                if (appender != null)
                {
                    ILayout? layout = null;
                    Level? threshold = null;

                    Type type = appender.GetType();
                    if (type == typeof(FileAppender))
                    {
                        layout = (appender as FileAppender)!.Layout;
                        threshold = (appender as FileAppender)!.Threshold;
                    }
                    else if (type == typeof(ConsoleAppender))
                    {
                        layout = (appender as ConsoleAppender)!.Layout;
                        threshold = (appender as ConsoleAppender)!.Threshold;
                    }
                    else if (type == typeof(ApplicationInsightsAppender))
                    {
                        layout = (appender as ApplicationInsightsAppender)!.Layout;
                        threshold = (appender as ApplicationInsightsAppender)!.Threshold;
                    }
                    
                    // Validate conversion pattern
                    if (layout != null)
                    {
                        if(!IsValidMessagePattern((layout as PatternLayout)!.ConversionPattern))
                        {
                            Logger.Warn($"Invalid message pattern configured for {appender.Name}");
                        }
                    }
                    else
                    {
                        Logger.Warn($"Appender {appender.Name} does not use PatternLayout");
                    }

                    // Validate log level
                    if (threshold == null)
                    {
                        Logger.Warn($"Log level for {appender.Name} is not declared");
                        return;
                    }

                    if (threshold.Value < Level.Debug.Value || threshold.Value > Level.Error.Value)
                    {
                        Logger.Warn($"Log level {threshold} for {appender.Name} is invalid");
                    }
                }
            }
        }
        
        private static bool IsValidMessagePattern(string pattern)
        {
            return pattern.Contains("%date") && 
                pattern.Contains("level") &&
                pattern.Contains("thread") &&
                pattern.Contains("%message") &&
                pattern.Contains("%newline");
        }
    }
}