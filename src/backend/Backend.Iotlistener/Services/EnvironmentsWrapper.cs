
using Backend.Iotlistener.Models.Enums;
using Backend.Iotlistener.Interfaces;

namespace Backend.Iotlistener.Services;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    public static readonly string _blobStreamerUrl = "BLOB_STREAMER_URL";
    public static readonly string _signingUrl = "SIGNING_URL";
    public static readonly string _rangeCalculateType = "RANGE_CALCULATE_TYPE";
    public static readonly string _rangePercent = "RANGE_PERCENT";
    public static readonly string _rangeBytes = "RANGE_BYTES";
    public static readonly string _messageTimeoutMinutes = "MESSAGE_TIMEOUT_MINUTES";
    public static readonly string _drainD2cQueues = "DRAIN_D2C_QUEUES";
    public static readonly string _iothubConnectionDeviceId = "IOTHUB_CONNECTION_DEVICE_ID";
    public static readonly string _iothubEventHubCompatiblePath = "IOTHUB_EVENT_HUB_COMPATIBLE_PATH";
    public static readonly string _iothubEventHubCompatibleEndpoint = "IOTHUB_EVENT_HUB_COMPATIBLE_ENDPOINT";
    public static readonly string _storageConnectionString = "STORAGE_CONNECTION_STRING";
    public static readonly string _blobContainerName = "BLOB_CONTAINER_NAME";
    public static readonly string _partitionId = "PARTITION_ID";

    public string blobStreamerUrl
    {
        get { return GetVariable(_blobStreamerUrl); }
    }
    public string signingUrl
    {
        get { return GetVariable(_signingUrl); }
    }
    public int rangePercent
    {
        get
        {
            return int.TryParse(GetVariable(_rangePercent), out int value) ? value : 0;
        }
    }
    public long rangeBytes
    {
        get
        {
            return int.TryParse(GetVariable(_rangeBytes), out int value) ? value : 0;
        }
    }
    public int messageTimeoutMinutes
    {
        get
        {
            return int.TryParse(GetVariable(_messageTimeoutMinutes), out int value) ? value : 60;
        }
    }
    public string drainD2cQueues
    {
        get { return GetVariable(_drainD2cQueues); }
    }
    public string iothubConnectionDeviceId
    {
        get { return GetVariable(_iothubConnectionDeviceId); }
    }
    public string iothubEventHubCompatiblePath
    {
        get { return GetVariable(_iothubEventHubCompatiblePath); }
    }
    public string iothubEventHubCompatibleEndpoint
    {
        get { return GetVariable(_iothubEventHubCompatibleEndpoint); }
    }
    public string storageConnectionString
    {
        get { return GetVariable(_storageConnectionString); }
    }
    public string blobContainerName
    {
        get { return GetVariable(_blobContainerName); }
    }
    public string partitionId
    {
        get { return GetVariable(_partitionId); }
    }
    public RangeCalculateType rangeCalculateType
    {
        get
        {
            return RangeCalculateType.TryParse(GetVariable(_rangeCalculateType), out RangeCalculateType value) ? value : RangeCalculateType.Bytes;

        }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
