namespace CloudPillar.Agent.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _deviceConnectionString = "DeviceConnectionString";
    private const string _transportType = "TransportType";
    private const string _periodicUploadInterval = "PeriodicUploadInterval";
    private const string _dpsScopeId = "DpsScopeId";
    private const string _globalDeviceEndpoint = "GlobalDeviceEndpoint";
    private const string _certificateExpiredDays = "CertificateExpiredDays";
    private const string _groupEnrollmentPrimaryKey ="GroupEnrollmentPrimaryKey";
    private const string _groupEnrollmentName = "GroupEnrollmentName";

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
    public string dpsScopeId
    {
        get { return GetVariable(_dpsScopeId); }
    }
    public string globalDeviceEndpoint
    {
        get { return GetVariable(_globalDeviceEndpoint); }
    }
    public string groupEnrollmentName
    {
        get { return GetVariable(_groupEnrollmentName); }
    }

    public string groupEnrollmentPrimaryKey
    {
        get { return GetVariable(_groupEnrollmentPrimaryKey); }
    }
    public int certificateExpiredDays
    {
        get
        {
            return int.TryParse(GetVariable(_certificateExpiredDays), out int value) ? value : 365;
        }
    }
    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
