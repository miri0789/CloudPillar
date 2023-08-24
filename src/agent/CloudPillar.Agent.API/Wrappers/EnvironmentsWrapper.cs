namespace CloudPillar.Agent.API.Wrappers;
public class EnvironmentsWrapper: IEnvironmentsWrapper
{
    private const string _deviceConnectionString = "DeviceConnectionString";
    private const string _transportType = "TransportType";

    public string deviceConnectionString
    {
        get { return GetVariable(_deviceConnectionString); }
    }
    public string transportType
    {
        get { return GetVariable(_transportType); }
    }


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
