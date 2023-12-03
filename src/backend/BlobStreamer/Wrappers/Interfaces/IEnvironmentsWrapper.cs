namespace Backend.BlobStreamer.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string storageConnectionString { get; }
    string blobContainerName { get; }
    string diagnosticsBlobContainerName { get; }
    string iothubConnectionString { get; }
    int messageExpiredMinutes { get; }
    int retryPolicyBaseDelay { get; }
    int retryPolicyExponent { get; }
}
