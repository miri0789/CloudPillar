namespace Backend.Keyholder.Services;
public interface IEnvironmentsWrapper
{
    string kubernetesServiceHost { get; }
    string signingPem { get; }
    string secretName { get; }
    string secretKey { get; }
    string iothubConnectionString { get; }

}
