

using Backender.Wrappers.Interfaces;

namespace Backender.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{

    private const string _serviceBusConnectionString = "SERVICE_BUS_CONNECTION_STRING";
    private const string _serviceBusUrls = "SERVICE_BUS_URLS";
    private const string _parallelCount = "PARALLEL_COUNT";
    private const string _maxLockDurationSeconds = "MAX_LOCK_DURATION_SECONDS";
    private const string _svcBackendUrl = "SVC_BACKEND_URL";
    private const string _higherPriorityGraceMS = "HIGHER_PRIORITY_GRACE_MS";
    private const string _noMessagesDelayMS = "NO_MESSAGES_DELAY_MS";
    private const string _requestTimeoutSeconds = "REQUEST_TIMEOUT_SECONDS";
    private const string _completionTopic = "COMPLETION_TOPIC";
    private const string _completionUrlBase = "COMPLETION_URL_BASE";
    private const string _defaultMaxdeliverycount = "DEFAULT_MAXDELIVERYCOUNT";
 
    private const int DEFAULLT_PARALLEL_COUNT = 1;
    private const int DEFAULLT_MAX_LOCK_DURATION_SECONDS = 30;
    private const int HIGHER_PRIORITY_GRACE_MS = 2000;
    private const int NO_MESSAGES_DELAY_MS = 5000;
    private const int REQUEST_TIMEOUT_SECONDS = 60;
    private const int MAX_DELIVERY_COUNT = 10;

    public string ServiceBusConnectionString
    {
        get { return GetVariable(_serviceBusConnectionString); }
    }
    public string[] ServiceBusUrls
    {
        get { return GetVariable(_serviceBusUrls)?.Split(';') ?? Array.Empty<string>(); }
    }
    public int ParallelCount
    {
        get { return int.TryParse(GetVariable(_parallelCount), out int value) ? value : DEFAULLT_PARALLEL_COUNT; }
    }
    public int MaxLockDurationSeconds
    {
        get { return int.TryParse(GetVariable(_maxLockDurationSeconds), out int value) ? value : DEFAULLT_MAX_LOCK_DURATION_SECONDS; }
    }
    public string SvcBackendUrl
    {
        get { return GetVariable(_svcBackendUrl); }
    }
    public int HigherPriorityGraceMS
    {
        get { return int.TryParse(GetVariable(_higherPriorityGraceMS), out int value) ? value : HIGHER_PRIORITY_GRACE_MS; }
    }
    public int NoMessagesDelayMS
    {
        get { return int.TryParse(GetVariable(_noMessagesDelayMS), out int value) ? value : NO_MESSAGES_DELAY_MS; }
    }
    public int RequestTimeoutSeconds
    {
        get { return int.TryParse(GetVariable(_requestTimeoutSeconds), out int value) ? value : REQUEST_TIMEOUT_SECONDS; }
    }
    public string CompletionTopic
    {
        get { return GetVariable(_completionTopic); }
    }
    public string CompletionUrlBase
    {
        get { return GetVariable(_completionUrlBase); }
    }
    public int defaultMaxdeliverycount
    {
        get { return int.TryParse(GetVariable(_defaultMaxdeliverycount), out int value) ? value : MAX_DELIVERY_COUNT; }
    }


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name)!;
    }

}
