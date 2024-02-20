using Backend.BlobStreamer.Enums;

namespace Backend.BlobStreamer.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string storageConnectionString { get; }
    string blobContainerName { get; }
    int messageExpiredMinutes { get; }
    int rangePercent { get; }
    long rangeBytes { get; }
    RangeCalculateType rangeCalculateType { get; }
}
