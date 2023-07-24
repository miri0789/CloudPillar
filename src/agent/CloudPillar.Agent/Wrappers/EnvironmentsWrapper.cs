namespace CloudPillar.Agent.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _deviceConnectionString = "DEVICE_CONNECTION_STRING";
    private const string _transportType = "TRANSPORT_TYPE";

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
