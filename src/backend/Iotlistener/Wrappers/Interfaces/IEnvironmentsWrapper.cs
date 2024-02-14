
namespace Backend.Iotlistener.Interfaces;

public interface IEnvironmentsWrapper
{
    string blobStreamerUrl { get; }
    string keyHolderUrl { get; }
    string beApiUrl { get; }
    int messageTimeoutMinutes { get; }
    string drainD2cQueues { get; }
    string iothubConnectionDeviceId { get; }
    string iothubEventHubCompatiblePath { get; }
    string iothubEventHubCompatibleEndpoint { get; }
    string storageConnectionString { get; }
    string blobContainerName { get; }
    string partitionId { get; }
}
