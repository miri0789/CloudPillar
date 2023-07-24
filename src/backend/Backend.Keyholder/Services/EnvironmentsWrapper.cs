using Backend.Keyholder.Interfaces;

namespace Backend.Keyholder.Services;
public class EnvironmentsWrapper: IEnvironmentsWrapper
{

    private const string _kubernetesServiceHost = "KUBERNETES_SERVICE_HOST";
    private const string _signingPem = "SIGNING_PEM";
    private const string _secretName = "SECRET_NAME";
    private const string _secretKey = "SECRET_KEY";
    private const string _iothubConnectionString = "IOTHUB_CONNECTION_STRING";

    public string kubernetesServiceHost
    {
        get { return GetVariable(_kubernetesServiceHost); }
    }
    public string signingPem
    {
        get { return GetVariable(_signingPem); }
    }
    public string secretName
    {
        get { return GetVariable(_secretName); }
    }
    public string secretKey
    {
        get { return GetVariable(_secretKey); }
    }
    public string iothubConnectionString
    {
        get { return GetVariable(_iothubConnectionString); }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

}
