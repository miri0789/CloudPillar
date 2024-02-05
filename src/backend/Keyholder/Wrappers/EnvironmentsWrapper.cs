

using Backend.Keyholder.Wrappers.Interfaces;

namespace Backend.Keyholder.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{

    private const string _kubernetesServiceHost = "KubernetesServiceHost";
    private const string _signingPem = "SigningPem";
    private const string _SecretVolumeMountPath = "SecretVolumeMountPath";
    private const string _DefaultSecretVolumeMountPath = "DefaultSecretVolumeMountPath";
    private const string _iothubConnectionString = "IothubConnectionString";

    private const string _dpsConnectionString = "DPSConnectionString";
    private const string _dpsIdScope = "DPSIdScope";
    private const string _globalDeviceEndpoint = "GlobalDeviceEndpoint";

    public string kubernetesServiceHost
    {
        get { return GetVariable(_kubernetesServiceHost); }
    }
    public string signingPem
    {
        get { return GetVariable(_signingPem); }
    }
    public string SecretVolumeMountPath
    {
        get { return GetVariable(_SecretVolumeMountPath); }
    }
    public string DefaultSecretVolumeMountPath
    {
        get { return GetVariable(_DefaultSecretVolumeMountPath); }
    }
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


    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
