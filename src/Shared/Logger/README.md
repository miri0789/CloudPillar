**Logger - Logging Conventions**
Logger class uses log4net library for managing the application logs.
The messages are logged to several loggers, based on the appender sections in log4net.config.

*Log Levels*
The supported log levels are:
* ERROR - When something unexpected occured, that blocked the process.
* WARN - When something invalid occured, but without blocking the process.
* INFO - For providing information.
* DEBUG - For debug/verbose messages in Development mode.

*Log Level Refresh*
The Logger supports changing the log level dynamically on runtime with no need to reset the service.
The log levels can be configured in Azure App Configuration, for Azure Application Insights and for other appenders.

*Azure App Configuration details*
* The App Configuration Connection String is set in appsettings.json, for example:
    "ConnectionStrings": {
        "AppConfig": "Endpoint=https://******"
    }

* The Configuration Keys to be added are:
    Log4Net:LogLevel:Default - for default log level {ERROR | INFO | WARN | DEBUG}
    Log4Net:LogLevel:AppInsights - for App Insights log level {ERROR | INFO | WARN | DEBUG}
    Log4Net:LogLevel:Appenders - for all other appenders log level {ERROR | INFO | WARN | DEBUG}
    Logging:LogLevel:RefreshInterval - for log level refresh interval in MS
    Logging:AppInsights:InstrumentationKey - for App Insights Instrumentation Key
    Logging:AppInsights:ConnectionString - for App Insights Connection String





