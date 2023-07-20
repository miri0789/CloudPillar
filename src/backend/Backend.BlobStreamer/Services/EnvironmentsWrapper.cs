namespace Backend.BlobStreamer.Services;
public class EnvironmentsWrapper: IEnvironmentsWrapper
{
    private const string _storageConnectionString = "STORAGE_CONNECTION_STRING";
    private const string _blobContainerName = "BLOB_CONTAINER_NAME";
    private const string _iothubConnectionString = "IOTHUB_CONNECTION_STRING";
    private const string _messageExpiredMinutes = "MESSAGE_EXPIRED_MINUTES";
    private const string _retryPolicyBaseDelay = "RETRY_POLICY_BASE_DELAY";
    private const string _retryPolicyExponent = "RETRY_POLICY_EXPONENT";

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
