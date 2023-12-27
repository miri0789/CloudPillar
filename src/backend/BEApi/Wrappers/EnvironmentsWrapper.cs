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


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
