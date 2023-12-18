﻿namespace Shared.Logger;

public interface ILoggerHandler
{
    void Error(string message, params object[] args);

    void Error(string message, Exception e, params object[] args);

    void Warn(string message, params object[] args);

    void Warn(string message, Exception e, params object[] args);

    void Info(string message, params object[] args);

    void Debug(string message, params object[] args);

    void Flush();

    void RefreshAppInsightsLogLevel(string logLevel);

    void RefreshAppendersLogLevel(string logLevel, bool init);
}