using System;
using System.Collections.Generic;
using log4net;
using log4net.Repository;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Shared.Logger;
using Shared.Logger.Wrappers;

namespace Shared.Logger.Tests;

public class LoggerHandlerTestFixture
{
    private const string m_appName = "appName";
    private ILoggerHandler m_target;
    private ILoggerHandler m_targetWithoutHttp;

    private ILoggerHandlerFactory m_LoggerFactoryMock;
    private IHttpContextAccessor m_httpContextAccessorMock;
    private ILog m_logMock;
    private ITelemetryClientWrapper m_telemetryClientWrapperMock;
    private HttpRequest m_request;
    private Dictionary<string, string> m_attributesDictionary;
    private static IDictionary<string, string> m_headersDictionary = new Dictionary<string, string>
            {
                { LoggerConstants.SESSION_USER_NAME, "Biosense"},
                { LoggerConstants.SESSION_TENANT_ID, "1234"}
            };
    private static IDictionary<string, string> m_queryDictionary = new Dictionary<string, string>
            {
                { LoggerConstants.SESSION_CASE_ID, "1.2.3.4" },
                { LoggerConstants.SESSION_CORRELATION_ID, "12345" }
            };
    private const string message = "Alice sent a letter to Bob at 12 O'clock!";
    private const string exceptionMessage = "Exception message";

    [SetUp]
    public void Setup()
    {
        InitHeadersAndQueryParams();
        m_LoggerFactoryMock = Substitute.For<ILoggerHandlerFactory>();
        var logRepositoryMock = Substitute.For<ILoggerRepository>();
        m_LoggerFactoryMock.CreateLogRepository(Arg.Any<string>()).Returns(logRepositoryMock);
        m_request = InitMockRequest(m_headersDictionary, m_queryDictionary);
        m_httpContextAccessorMock = Substitute.For<IHttpContextAccessor>();
        var HttpContextMock = Substitute.For<HttpContext>();
        m_httpContextAccessorMock.HttpContext.Returns(HttpContextMock);
        m_httpContextAccessorMock.HttpContext.Request.Returns(m_request);
        m_attributesDictionary = m_headersDictionary.Concat(m_queryDictionary).ToDictionary(entry => entry.Key, entry => entry.Value);
        m_telemetryClientWrapperMock = Substitute.For<ITelemetryClientWrapper>();
        m_LoggerFactoryMock.CreateTelemetryClient(Arg.Any<string>()).Returns(m_telemetryClientWrapperMock);
        m_logMock = Substitute.For<ILog>();
        m_target = CreateTarget(m_LoggerFactoryMock, m_httpContextAccessorMock, m_logMock, "appInsightKey", "log4net.config", m_appName, "connectionString");
        m_targetWithoutHttp = CreateTarget(m_LoggerFactoryMock, null, m_logMock, "appInsightKey", "log4net.config", m_appName, "connectionString", false);
    }

    private void InitHeadersAndQueryParams()
    {
        m_headersDictionary = new Dictionary<string, string>
            {
                { LoggerConstants.SESSION_USER_NAME, "Biosense"},
                { LoggerConstants.SESSION_TENANT_ID, "1234"}
            };
        m_queryDictionary = new Dictionary<string, string>
            {
                { LoggerConstants.SESSION_CASE_ID, "1.2.3.4" },
                { LoggerConstants.SESSION_CORRELATION_ID, "12345" }
            };
    }

    private HttpRequest InitMockRequest(IDictionary<string, string> headersDictionary, IDictionary<string, string> queryDictionary)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var header in headersDictionary)
        {
            httpContext.Request.Headers.Add(header.Key.ToString(), header.Value.ToString());
        }
        foreach (var param in queryDictionary)
        {
            httpContext.Request.QueryString = httpContext.Request.QueryString.Add(param.Key.ToString(), param.Value.ToString());
        }
        return httpContext.Request;
    }

    private static ILoggerHandler CreateTarget(
        ILoggerHandlerFactory LoggerFactory,
        IHttpContextAccessor httpContextAccessor,
        ILog log,
        string appInsightKey,
        string log4NetName,
        string appName,
        string connectionString,
        bool hasHttpContext = true)
    {
        return new LoggerHandler(LoggerFactory, httpContextAccessor, log, appInsightKey, log4NetName, appName, connectionString, hasHttpContext);
    }

    #region Error
    [Test]
    public void Error_FormattedMessageWithArgs_SendToLogger()
    {
        m_target.Error("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Error(message);
    }

    [Test]
    public void Error_FormattedMessageWithoutArgs_SendToLogger()
    {
        m_target.Error("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Error(message);
    }

    [Test]
    public void Error_WithHttpContext_SendMessageWithArgsToAppInsights()
    {
        m_target.Error("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithHttpContext_SendMessageWithoutArgsToAppInsights()
    {
        m_target.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithHttpContext_SendErrorSeverityToAppInsights()
    {
        m_target.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Error, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithHttpContext_SendPropertiesToAppInsights()
    {
        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }

    [Test]
    public void Error_WithoutHttpContext_SendMessageWithArgsToAppInsights()
    {
        m_targetWithoutHttp.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithoutHttpContext_SendMessageWithoutArgsToAppInsights()
    {
        m_targetWithoutHttp.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithoutHttpContext_SendErrorSeverityToAppInsights()
    {
        m_targetWithoutHttp.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Error, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithoutHttpContext_SendPropertiesToAppInsights()
    {
        var expected = new Dictionary<string, string>
        {
            { "applicationName", m_appName }
        };
        m_targetWithoutHttp.Error(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }

    [Test]
    public void Error_WithExceptionAndArgs_SendToLoggerFormattedError()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));
        var error = string.Join(Environment.NewLine, string.Format("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12), exception);

        m_target.Error("{0} sent a letter to {1} at {2} O'clock!", exception, "Alice", "Bob", 12);

        m_logMock.Received(1).Error(error);
    }

    [Test]
    public void Error_WithException_SendToLoggerFormattedError()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));
        var error = string.Join(Environment.NewLine, string.Format(message), exception);

        m_target.Error(message, exception);

        m_logMock.Received(1).Error(error);
    }

    [Test]
    public void Error_WithExceptionAndHttpContext_SendExceptionWithoutArgsToAppInsights()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));

        m_target.Error(message, exception);

        m_telemetryClientWrapperMock.Received(1).TrackException(exception, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithExceptionAndhHttpContextAndArgs_SendExceptionToAppInsights()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));

        m_target.Error("{0} sent a letter to {1} at {2} O'clock!", exception, "Alice", "Bob", 12);

        m_telemetryClientWrapperMock.Received(1).TrackException(exception, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithExceptionAndHttpContext_SendPropertiesToAppInsights()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));
        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Error(message, exception);

        m_telemetryClientWrapperMock.Received(1).TrackException(Arg.Any<Exception>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }

    [Test]
    public void Error_WithExceptionWithoutHttpContext_SendExceptionToAppInsights()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));

        m_targetWithoutHttp.Error(message, exception);

        m_telemetryClientWrapperMock.Received(1).TrackException(exception, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Error_WithExceptionWithoutHttpContext_SendPropertiesToAppInsights()
    {
        var exception = new Exception(exceptionMessage, new Exception(exceptionMessage));
        var expected = new Dictionary<string, string>
        {
            { "applicationName", m_appName }
        };
        m_targetWithoutHttp.Error(message, exception);

        m_telemetryClientWrapperMock.Received(1).TrackException(Arg.Any<Exception>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }

    #endregion Error

    #region Warn
    [Test]
    public void Warn_FormattedMessageWithArgs_SendToLogger()
    {
        m_target.Warn("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Warn(message);
    }

    [Test]
    public void Warn_FormattedMessageWithoutArgs_SendToLogger()
    {
        m_target.Warn("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Warn(message);
    }

    [Test]
    public void Warn_WithHttpContext_SendMessageWithArgsToAppInsights()
    {
        m_target.Warn("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithHttpContext_SendMessageWithoutArgsToAppInsights()
    {
        m_target.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithHttpContext_SendWarningSeverityToAppInsights()
    {
        m_target.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Warning, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithHttpContext_SendPropertiesToAppInsights()
    {
        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }



    [Test]
    public void Warn_WithoutHttpContext_SendMessageWithToAppInsights()
    {
        m_targetWithoutHttp.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithoutHttpContext_SendMessageWithoutToAppInsights()
    {
        m_targetWithoutHttp.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithoutHttpContext_SendWarningSeverityToAppInsights()
    {
        m_targetWithoutHttp.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Warning, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Warn_WithoutHttpContext_SendPropertiesToAppInsights()
    {
        var expected = new Dictionary<string, string>
        {
            { "applicationName", m_appName }
        };
        m_targetWithoutHttp.Warn(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }
    #endregion Warn

    #region Info
    [Test]
    public void Info_FormattedMessageWithArgs_SendToLogger()
    {
        m_target.Info("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Info(message);
    }

    [Test]
    public void Info_FormattedMessageWithoutArgs_SendToLogger()
    {
        m_target.Info("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Info(message);
    }

    [Test]
    public void Info_WithHttpContext_SendMessageWithToAppInsights()
    {
        m_target.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithHttpContext_SendMessageWithoutToAppInsights()
    {
        m_target.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithHttpContext_SendInformationSeverityToAppInsights()
    {
        m_target.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Information, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithHttpContext_SendPropertiesToAppInsights()
    {
        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }

    [Test]
    public void Info_WithHttpContextNotAllProperties_SendPropertiesToAppInsights()
    {
        m_headersDictionary.Remove(LoggerConstants.SESSION_TENANT_ID);
        m_queryDictionary.Remove(LoggerConstants.SESSION_CASE_ID);
        m_request = InitMockRequest(m_headersDictionary, m_queryDictionary);
        m_httpContextAccessorMock.HttpContext.Request.Returns(m_request);

        m_attributesDictionary[LoggerConstants.SESSION_CASE_ID] = "";
        m_attributesDictionary[LoggerConstants.SESSION_TENANT_ID] = "";

        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }



    [Test]
    public void Info_WithoutHttpContext_SendMessageWithToAppInsights()
    {
        m_targetWithoutHttp.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithoutHttpContext_SendMessageWithoutToAppInsights()
    {
        m_targetWithoutHttp.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithoutHttpContext_SendInformationSeverityToAppInsights()
    {
        m_targetWithoutHttp.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Information, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Info_WithoutHttpContext_SendPropertiesToAppInsights()
    {
        var expected = new Dictionary<string, string>
        {
            { "applicationName", m_appName }
        };
        m_targetWithoutHttp.Info(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }
    #endregion Info

    #region Debug
    [Test]
    public void Debug_FormattedMessageWithArgs_SendToLogger()
    {
        m_target.Debug("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Debug(message);
    }

    [Test]
    public void Debug_FormattedMessageWithoutArgs_SendToLogger()
    {
        m_target.Debug("{0} sent a letter to {1} at {2} O'clock!", "Alice", "Bob", 12);

        m_logMock.Received(1).Debug(message);
    }

    [Test]
    public void Debug_WithHttpContext_SendMessageWithToAppInsights()
    {
        m_target.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithHttpContext_SendMessageWithoutToAppInsights()
    {
        m_target.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithHttpContext_SendVerboseSeverityToAppInsights()
    {
        m_target.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Verbose, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithHttpContext_SendPropertiesToAppInsights()
    {
        var expected = m_attributesDictionary;

        expected.Add("applicationName", m_appName);

        m_target.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message , Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }



    [Test]
    public void Debug_WithoutHttpContext_SendMessageWithToAppInsights()
    {
        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithoutHttpContext_SendMessageWithoutToAppInsights()
    {
        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(), Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithoutHttpContext_SendVerboseSeverityToAppInsights()
    {
        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Verbose, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void Debug_WithoutHttpContext_SendPropertiesToAppInsights()
    {
        var expected = new Dictionary<string, string>
        {
            { "applicationName", m_appName }
        };
        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, Arg.Any<SeverityLevel>(),
            Arg.Is<IDictionary<string, string>>(x => expected.OrderBy(kvp => kvp.Key).SequenceEqual(x.OrderBy(kvp => kvp.Key))));
    }
    #endregion Debug

    [Test]
    public void RefreshAppInsightsLogLevel_LogMessage()
    {
        m_targetWithoutHttp.RefreshAppInsightsLogLevel("DEBUG");

        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Verbose, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void RefreshAppInsightsLogLevel_DoNotLogMessage()
    { 
        m_targetWithoutHttp.RefreshAppInsightsLogLevel("WARN");

        m_targetWithoutHttp.Debug(message);

        m_telemetryClientWrapperMock.DidNotReceive().TrackTrace(
            message,
            Arg.Any<SeverityLevel>(),
            Arg.Any<IDictionary<string, string>>()
        );
    }

    [Test]
    public void RefreshAppendersLogLevel_LogMessage()
    {
        m_targetWithoutHttp.RefreshAppendersLogLevel("ERROR");

        m_targetWithoutHttp.Error(message);

        m_logMock.Received(1).Error(message);
    }

    [Test]
    public void RefreshAppendersLogLevel_DoNotLogMessage()
    { 
        m_targetWithoutHttp.RefreshAppendersLogLevel("ERROR");

        m_targetWithoutHttp.Info(message);

        m_logMock.DidNotReceiveWithAnyArgs().Error(Arg.Any<string>());
    }

    [Test]
    public void RefreshAppInsightsLogLevel_InvalidLogLevel()
    {
        var logLevel = "InvalidLogLevel";
        var message = $"Trying to set invalid log level: {logLevel}";
        
        m_targetWithoutHttp.RefreshAppInsightsLogLevel(logLevel);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Warning, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void RefreshAppInsightsLogLevel_ValidLogLevel()
    {
        var logLevel = "INFO";
        var message = $"App Insights Log Level changed to {logLevel}";
        
        m_targetWithoutHttp.RefreshAppInsightsLogLevel(logLevel);

        m_telemetryClientWrapperMock.Received(1).TrackTrace(message, SeverityLevel.Information, Arg.Any<IDictionary<string, string>>());
    }

    [Test]
    public void RefreshAppendersLogLevel_InvalidLogLevel()
    {
        var logLevel = "InvalidLogLevel";
        var message = $"Trying to set invalid log level: {logLevel}";
        
        m_targetWithoutHttp.RefreshAppendersLogLevel(logLevel);

        m_logMock.Received(1).Warn(message);
    }

    [Test]
    public void RefreshAppendersLogLevel_ValidLogLevel()
    {
        var logLevel = "DEBUG";
        
        m_targetWithoutHttp.RefreshAppendersLogLevel(logLevel);

        m_logMock.Received().Info(Arg.Is<string>(msg => msg.Contains($"Log Level changed to {logLevel}")));
    }

}