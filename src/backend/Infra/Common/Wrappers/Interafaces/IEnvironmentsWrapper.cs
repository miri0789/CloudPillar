namespace Backend.Infra.Wrappers;
public interface ICommonEnvironmentsWrapper
{
    int retryPolicyBaseDelay { get; }
    int retryPolicyExponent { get; }    
    string iothubConnectionString { get; }
}
