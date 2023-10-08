namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string kubernetesServiceHost { get; }
    string signingPem { get; }
    string secretName { get; }
    string secretKey { get; }
    string iothubConnectionString { get; }
    string dpsConnectionString { get; }
    string dpsIdScope { get; }
    int certificateExpiredDays { get; }

}
