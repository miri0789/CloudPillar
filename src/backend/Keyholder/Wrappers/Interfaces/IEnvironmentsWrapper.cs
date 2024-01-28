namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string kubernetesServiceHost { get; }
    string signingPem { get; }
    string SecretVolumeMountPath { get; }
    string iothubConnectionString { get; }
    string dpsConnectionString { get; }
    string dpsIdScope { get; }
    string globalDeviceEndpoint { get; }
}
