namespace Shared.Logger;

public class LoggerConstants
{
    public static readonly string SESSION_USER_NAME = "userName";
    public static readonly string SESSION_CASE_ID = "caseId";
    public static readonly string SESSION_CORRELATION_ID = "correlationId";
    public static readonly string SESSION_TENANT_ID = "tenantId";
    public static readonly string APPLICATION_NAME = "applicationName";
    public static readonly string LOG_LEVEL_DEFAULT_CONFIG = "Log4Net:LogLevel:Default";
    public static readonly string LOG_LEVEL_APPINSIGHTS_CONFIG = "Log4Net:LogLevel:AppInsights";
    public static readonly string LOG_LEVEL_APPENDERS_CONFIG = "Log4Net:LogLevel:Appenders";
    public static readonly string LOG_LEVEL_INTERVAL_CONFIG = "Logging:LogLevel:RefreshInterval";
    public static readonly string LOG_LEVEL_DEFAULT_THRESHOLD = "DEBUG"; 
    public static readonly string APPINSIGHTS_INSTRUMENTATION_KEY_CONFIG = "Logging:AppInsights:InstrumentationKey";
    public static readonly string APPINSIGHTS_CONNECTION_STRING_CONFIG = "Logging:AppInsights:ConnectionString";   
}
