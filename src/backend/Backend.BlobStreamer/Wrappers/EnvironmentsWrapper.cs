using Backend.BlobStreamer.Interfaces;

namespace Backend.BlobStreamer.Wrappers;
public class EnvironmentsWrapper: IEnvironmentsWrapper
{
    private const string _storageConnectionString = "StorageConnectionString";
    private const string _blobContainerName = "BlobContainerName";
    private const string _iothubConnectionString = "IothubConnectionString";
    private const string _messageExpiredMinutes = "MessageExpiredMinutes";
    private const string _retryPolicyBaseDelay = "RetryPolicyBaseDelay";
    private const string _retryPolicyExponent = "RetryPolicyExponent";

    public string storageConnectionString
    {
        get { return GetVariable(_storageConnectionString); }
    }
    public string blobContainerName
    {
        get { return GetVariable(_blobContainerName); }
    }
    public string iothubConnectionString
    {
        get { return GetVariable(_iothubConnectionString); }
    }
    public int messageExpiredMinutes
    {
        get
        {
            return int.TryParse(GetVariable(_messageExpiredMinutes), out int value) ? value : 60;
        }
    }
    public int retryPolicyBaseDelay
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyBaseDelay), out int value) ? value : 1;
        }
    }
    public int retryPolicyExponent
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyExponent), out int value) ? value : 1;
        }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
