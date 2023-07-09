using Microsoft.Extensions.Hosting;

namespace shared.Logger;

public interface ILogger
{
    public void Error(string message, params object[] args);

    public void Error(string message, Exception e, params object[] args);

    public void Warn(string message, params object[] args);

    public void Warn(string message, Exception e, params object[] args);

    public void Info(string message, params object[] args);

    public void Debug(string message, params object[] args);

    public void Flush();

    public void RefreshAppInsightsLogLevel(string logLevel);

    public void RefreshAppendersLogLevel(string logLevel);

    public IHostBuilder GetLoggerHostBuilder(string[] args);
}