
using Backend.Iotlistener.Models.Enums;
using Backend.Iotlistener.Interfaces;

namespace Backend.Iotlistener.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    public static readonly string _blobStreamerUrl = "BlobStreamerUrl";
    public static readonly string _keyHolderUrl = "KeyHolderUrl";
    public static readonly string _beApiUrl = "BEApiUrl";
    public static readonly string _rangeCalculateType = "RangeCalculateType";
    public static readonly string _rangePercent = "RangePercent";
    public static readonly string _rangeBytes = "RangeBytes";
    public static readonly string _messageTimeoutMinutes = "MessageTimeoutMinutes";
    public static readonly string _drainD2cQueues = "DrainD2cQueues";
    public static readonly string _iothubConnectionDeviceId = "IothubConnectionDeviceId";
    public static readonly string _iothubEventHubCompatiblePath = "IothubEventHubCompatiblePath";
    public static readonly string _iothubEventHubCompatibleEndpoint = "IothubEventHubCompatibleEndpoint";
    public static readonly string _storageConnectionString = "StorageConnectionString";
    public static readonly string _blobContainerName = "BlobContainerName";
    public static readonly string _partitionId = "PartitionId";

    public string blobStreamerUrl
    {
        get { return GetVariable(_blobStreamerUrl); }
    }
    public string keyHolderUrl
    {
        get { return GetVariable(_keyHolderUrl); }
    }
    public string beApiUrl
    {
        get { return GetVariable(_beApiUrl); }
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
