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
    private const string _keyHolderUrl = "KeyHolderUrl";
    private const string _blobStreamerUrl = "BlobStreamerUrl";
    public static readonly string _rangeCalculateType = "RangeCalculateType";
    public static readonly string _rangePercent = "RangePercent";
    public static readonly string _rangeBytes = "RangeBytes";



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

    public string keyHolderUrl
    {
        get { return GetVariable(_keyHolderUrl); }
    }

    public string blobStreamerUrl
    {
        get { return GetVariable(_blobStreamerUrl); }
    }
    public RangeCalculateType rangeCalculateType
    {
        get
        {
            return RangeCalculateType.TryParse(GetVariable(_rangeCalculateType), out RangeCalculateType value) ? value : RangeCalculateType.Bytes;

        }
    }
    public int rangePercent
    {
        get
        {
            return int.TryParse(GetVariable(_rangePercent), out int value) ? value : 0;
        }
    }
    public long rangeBytes
    {
        get
        {
            return int.TryParse(GetVariable(_rangeBytes), out int value) ? value : 0;
        }
    }
    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
