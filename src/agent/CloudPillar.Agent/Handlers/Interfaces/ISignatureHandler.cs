namespace CloudPillar.Agent.Handlers;


public interface ISignatureHandler
{
    Task<bool> VerifySignatureAsync(byte[] dataToVerify, string signatureString);
    Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken);
}