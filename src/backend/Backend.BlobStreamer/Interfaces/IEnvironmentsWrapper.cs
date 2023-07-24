namespace Backend.BlobStreamer.Services;
public interface IEnvironmentsWrapper
{
    string storageConnectionString { get; }
    string blobContainerName { get; }
    string iothubConnectionString { get; }
    int messageExpiredMinutes { get; }
    int retryPolicyBaseDelay { get; }
    int retryPolicyExponent { get; }
}
