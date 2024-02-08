namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string kubernetesServiceHost { get; }
    string SecretVolumeMountPath { get; }
    string DefaultSecretVolumeMountPath { get; }
    string iothubConnectionString { get; }
    string dpsConnectionString { get; }
    string dpsIdScope { get; }
    string globalDeviceEndpoint { get; }
}
