namespace Backend.Infra.Common.Wrappers.Interfaces;
public interface ICommonEnvironmentsWrapper
{
    int retryPolicyBaseDelay { get; }
    int retryPolicyExponent { get; }
    string iothubConnectionString { get; }
    string serviceBusConnectionString { get; }
    string queueName { get; }
    string keyHolderUrl { get; }
    string blobStreamerUrl { get; }

}
