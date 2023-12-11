namespace CloudPillar.Agent.Handlers;

public interface ISignatureHandler
{
    Task InitPublicKeyAsync();
    Task<bool> VerifySignatureAsync(string message, string signatureString);
    Task<bool> VerifyFileSignatureAsync(string filePath, string signature);
    Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken);
}