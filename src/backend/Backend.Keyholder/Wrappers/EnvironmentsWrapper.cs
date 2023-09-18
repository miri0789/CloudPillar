using Backend.Keyholder.Interfaces;

namespace Backend.Keyholder.Wrappers;
public class EnvironmentsWrapper: IEnvironmentsWrapper
{

    private const string _kubernetesServiceHost = "KubernetesServiceHost";
    private const string _signingPem = "SigningPem";
    private const string _secretName = "SecretName";
    private const string _secretKey = "SecretKey";
    private const string _iothubConnectionString = "IothubConnectionString";

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
