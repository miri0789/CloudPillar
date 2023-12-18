namespace Backend.BlobStreamer.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string storageConnectionString { get; }
    string blobContainerName { get; }
    int messageExpiredMinutes { get; }
}
