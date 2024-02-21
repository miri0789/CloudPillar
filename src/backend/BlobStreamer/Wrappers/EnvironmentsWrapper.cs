using Backend.BlobStreamer.Wrappers.Interfaces;

namespace Backend.BlobStreamer.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _storageConnectionString = "StorageConnectionString";
    private const string _blobContainerName = "BlobContainerName";
    private const string _messageExpiredMinutes = "MessageExpiredMinutes";
    private const string _keyHolderUrl = "KeyHolderUrl";

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
    public string keyHolderUrl
    {
        get { return GetVariable(_keyHolderUrl); }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
