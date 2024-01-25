﻿

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

    public string serviceBusConnectionString
    {
        get { return GetVariable(_serviceBusConnectionString) ?? "Endpoint=sb://tryperiodicqueue.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XuvbHsyCDS74pTiizel0GjPLTNo8w8ftl+ASbDLFCpE="; }
    }
    public string[] serviceBusUrls
    {
        get { return GetVariable(_serviceBusUrls)?.Split(';') ?? "periority1;periority2;periority3".Split(';'); }
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


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name)!;
    }

}