namespace CloudPillar.Agent.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _deviceConnectionString = "DeviceConnectionString";
    private const string _transportType = "TransportType";
    private const string _periodicUploadInterval = "PeriodicUploadInterval";

    public string deviceConnectionString
    {
        get { return GetVariable(_deviceConnectionString); }
    }
    public string transportType
    {
        get { return GetVariable(_transportType); }
    }
    public string periodicUploadInterval
    {
        get { return GetVariable(_periodicUploadInterval); }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
