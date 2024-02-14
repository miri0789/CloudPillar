using Backend.BlobStreamer.Enums;
using Backend.BlobStreamer.Wrappers.Interfaces;

namespace Backend.BlobStreamer.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _storageConnectionString = "StorageConnectionString";
    private const string _blobContainerName = "BlobContainerName";
    private const string _messageExpiredMinutes = "MessageExpiredMinutes";
    public static readonly string _rangeCalculateType = "RangeCalculateType";
    public static readonly string _rangePercent = "RangePercent";
    public static readonly string _rangeBytes = "RangeBytes";

    public string storageConnectionString
    {
        get { return GetVariable(_storageConnectionString); }
    }
    public string blobContainerName
    {
        get { return GetVariable(_blobContainerName); }
    }
    public int messageExpiredMinutes
    {
        get
        {
            return int.TryParse(GetVariable(_messageExpiredMinutes), out int value) ? value : 60;
        }
    }
    public RangeCalculateType rangeCalculateType
    {
        get
        {
            return RangeCalculateType.TryParse(GetVariable(_rangeCalculateType), out RangeCalculateType value) ? value : RangeCalculateType.Bytes;

        }
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

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
