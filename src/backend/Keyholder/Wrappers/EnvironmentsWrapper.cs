﻿

using Backend.Keyholder.Wrappers.Interfaces;

namespace Backend.Keyholder.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{

    private const string _kubernetesServiceHost = "KubernetesServiceHost";
    private const string _signingPem = "SigningPem";
    private const string _secretName = "SecretName";
    private const string _secretKey = "SecretKey";
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