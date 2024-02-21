using Backend.Infra.Common.Wrappers.Interfaces;
namespace Backend.Infra.Wrappers;
public class CommonEnvironmentsWrapper : ICommonEnvironmentsWrapper
{
    private const string _iothubConnectionString = "IothubConnectionString";
    private const string _retryPolicyBaseDelay = "RetryPolicyBaseDelay";
    private const string _retryPolicyExponent = "RetryPolicyExponent";
    private const string _serviceBusConnectionString = "ServiceBusConnectionString";
    private const string _queueName = "QueueName";
    private const string _keyHolderUrl = "KeyHolderUrl";
    private const string _blobStreamerUrl = "BlobStreamerUrl";

    public int retryPolicyBaseDelay
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyBaseDelay), out int value) ? value : 20;
        }
    }
    public int retryPolicyExponent
    {
        get
        {
            return int.TryParse(GetVariable(_retryPolicyExponent), out int value) ? value : 15;
        }
    }

    public string iothubConnectionString
    {
        get { return GetVariable(_iothubConnectionString); }
    }

    public string serviceBusConnectionString
    {
        get { return GetVariable(_serviceBusConnectionString); }
    }

    public string queueName
    {
        get { return GetVariable(_queueName); }
    }

    public string keyHolderUrl
    {
        get { return GetVariable(_keyHolderUrl); }
    }

    public string blobStreamerUrl
    {
        get { return GetVariable(_blobStreamerUrl); }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
