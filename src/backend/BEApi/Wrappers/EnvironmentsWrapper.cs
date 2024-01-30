using Backend.BEApi.Wrappers.Interfaces;

namespace Backend.BEApi.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _iothubConnectionString = "IothubConnectionString";
    private const string _dpsConnectionString = "DPSConnectionString";
    private const string _dpsIdScope = "DPSIdScope";
    private const string _globalDeviceEndpoint = "GlobalDeviceEndpoint";
    private const string _expirationCertificatePercent = "ExpirationCertificatePercent";
    private const string _maxCountDevices = "MaxCountDevices";
    private const string _storageConnectionString = "StorageConnectionString";
    private const string _blobContainerName = "BlobContainerName";
    private const string _keyHolderUrl = "KeyHolderUrl";
    private const string _blobStreamerUrl = "BlobStreamerUrl";
    public string iothubConnectionString
    {
        get { return GetVariable(_iothubConnectionString); }
    }

    public string dpsConnectionString
    {
        get { return GetVariable(_dpsConnectionString); }
    }

    public string dpsIdScope
    {
        get { return GetVariable(_dpsIdScope); }
    }

    public string globalDeviceEndpoint
    {
        get { return GetVariable(_globalDeviceEndpoint); }
    }

    public double expirationCertificatePercent
    {
        get { return double.Parse(GetVariable(_expirationCertificatePercent)); }
    }

    public int maxCountDevices
    {
        get { return int.Parse(GetVariable(_maxCountDevices)); }
    }
    public string storageConnectionString
    {
        get { return GetVariable(_storageConnectionString); }
    }

    public string blobContainerName
    {
        get { return GetVariable(_blobContainerName); }
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
