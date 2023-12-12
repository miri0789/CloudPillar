using System;
using System.Reflection;
using log4net;
using log4net.Core;
using log4net.Layout;
using log4net.Appender;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace CloudPillar.Agent.Handlers.Logger
{
    public static class Log4netConfigurationValidator
    {
        public static void ValidateConfiguration(ILoggerHandler logger)
        {
            var appenders = LogManager.GetRepository(Assembly.GetExecutingAssembly()).GetAppenders();
            
            foreach (var appender in appenders)
            {
                if (appender == null)
                {
                    continue;
                }
                logger.Debug($"Validating appender {appender.Name}");
                var appenderType = appender.GetType();

                // Validate conversion pattern
                ILayout? layout = null;

                var layoutProperty = appenderType.GetProperty("Layout");
                if (layoutProperty?.CanRead == true)
                {
                    layout = layoutProperty.GetValue(appender) as PatternLayout;
                }

                if (layout != null)
                {
                    var layoutType = layout.GetType();
                    var conversionPatternProperty = layoutType.GetProperty("ConversionPattern");

                    if (conversionPatternProperty?.CanRead == true)
                    {
                        string? currentConversionPattern = conversionPatternProperty.GetValue(layout) as string;
                        if(!IsValidMessagePattern(currentConversionPattern))
                        {
                            logger.Warn($"Invalid message pattern configured for {appender.Name}");
                        }
                    }
                }

                // Validate log level
                var thresholdProperty = appenderType.GetProperty("Threshold");

                if (thresholdProperty?.CanWrite == true)
                {
                    Level? threshold = thresholdProperty.GetValue(appender) as Level;

                    if (threshold?.Value < Level.Debug.Value)
                    {
                        logger.Warn($"Log level {threshold} for {appender.Name} is too low, setting to Debug");
                        thresholdProperty.SetValue(appender, Level.Debug, null);
                        ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                    }
                    else if (threshold?.Value > Level.Error.Value)
                    {
                        logger.Warn($"Log level {threshold} for {appender.Name} is too high, setting to Error");
                        thresholdProperty.SetValue(appender, Level.Error, null);
                        ((Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
                    }
                }
            }
        }
        
        private static bool IsValidMessagePattern(string? pattern)
        {
            return !string.IsNullOrEmpty(pattern) &&
                pattern.Contains("%date") && 
                pattern.Contains("level") &&
                pattern.Contains("thread") &&
                pattern.Contains("%message") &&
                pattern.Contains("%newline");
        }
    }
}