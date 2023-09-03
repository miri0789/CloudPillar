using log4net;
using log4net.Appender;
using log4net.Repository;

namespace Shared.Logger;
public static class Log4netExtentions
{
    public static bool IsAppenderDefined<TAppender>(ILoggerRepository repository) where TAppender : IAppender
    {
        return repository.GetAppenders().OfType<TAppender>().Any();
    }

    // Check if an appender of the specified type is referenced in the root logger
    public static bool IsAppenderInRoot<TAppender>(ILoggerRepository repository) where TAppender : IAppender
    {
        IAppender[] appendersInRoot = LogManager.GetLogger(repository.Name).Logger.Repository.GetAppenders();
        return appendersInRoot.OfType<TAppender>().Any();
    }

    public static bool IsAppenderExists<TAppender>(ILoggerRepository repository) where TAppender : IAppender
    {
        return IsAppenderDefined<TAppender>(repository) && IsAppenderInRoot<TAppender>(repository);
    }

    public static TAppender? GetAppender<TAppender>(ILoggerRepository repository) where TAppender : IAppender
    {
        return  repository.GetAppenders().OfType<TAppender>().FirstOrDefault();
    }
}