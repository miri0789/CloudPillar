
using Backend.Iotlistener.Models.Enums;

namespace Backend.Iotlistener.Interfaces;
public interface IEnvironmentsWrapper
{

    string blobStreamerUrl { get; }
    string signingUrl { get; }
    int rangePercent { get; }
    long rangeBytes { get; }
    int messageTimeoutMinutes { get; }
    string drainD2cQueues { get; }
    string iothubConnectionDeviceId { get; }
    string iothubEventHubCompatiblePath { get; }
    string iothubEventHubCompatibleEndpoint { get; }
    string storageConnectionString { get; }
    string blobContainerName { get; }
    RangeCalculateType rangeCalculateType { get; }
    string partitionId { get; }


}
