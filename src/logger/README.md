**Logger - Logging Conventions**
Logger class uses log4net library for managing the application logs.
The messages are logged to several loggers, based on log4net.config appender sections.

*Log Levels*
The supported log levels are:
* ERROR - When something unexpected occured, that blocked the process.
* WARNING - When something invalid occured, but without blocking the process.
* INFO - For providing information.
* DEBUG - For debug/verbose messages in Development mode.

*Log Level Refresh*
The Logger supports changing the log level dynamically on runtime with no need to reset the service.
The log levels can be configured in Azure App Configuration, for Azure Application Insights and for other appenders.

Azure App Configuration details:
* The Connection String is set in appsettings.json:
    "ConnectionStrings": {
        "AppConfig": "Endpoint=https://iot-app-config-svc.azconfig.io;Id=N6SW;Secret=PHj7anTdfJ2tuCvw3bSt4rA76Ff97uY2lE5bDv2bzMk="
    }

* 




