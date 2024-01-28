

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

    public string serviceBusConnectionString
    {
        get { return GetVariable(_serviceBusConnectionString); }
    }
    public string[] serviceBusUrls
    {
        get { return GetVariable(_serviceBusUrls)?.Split(';') ?? Array.Empty<string>(); }
    }
    public int parallelCount
    {
        get { return int.TryParse(GetVariable(_parallelCount), out int value) ? value : 1; }
    }
    public int maxLockDurationSeconds
    {
        get { return int.TryParse(GetVariable(_maxLockDurationSeconds), out int value) ? value : 30; }
    }
    public string svcBackendUrl
    {
        get { return GetVariable(_svcBackendUrl); }
    }
    public int higherPriorityGraceMS
    {
        get { return int.TryParse(GetVariable(_higherPriorityGraceMS), out int value) ? value : 2000; }
    }
    public int noMessagesDelayMS
    {
        get { return int.TryParse(GetVariable(_noMessagesDelayMS), out int value) ? value : 5000; }
    }
    public int requestTimeoutSeconds
    {
        get { return int.TryParse(GetVariable(_requestTimeoutSeconds), out int value) ? value : 60; }
    }
    public string completionTopic
    {
        get { return GetVariable(_completionTopic); }
    }
    public string completionUrlBase
    {
        get { return GetVariable(_completionUrlBase); }
    }


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name)!;
    }

}
