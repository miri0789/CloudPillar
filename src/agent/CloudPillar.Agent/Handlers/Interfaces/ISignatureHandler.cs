namespace CloudPillar.Agent.Handlers;

public interface ISignatureHandler
{   
    Task<bool> VerifyFileSignatureAsync(string filePath, string signature);
    Task<bool> VerifySignatureAsync(byte[] dataToVerify, string signatureString);
    Task SendSignTwinKeyEventAsync(string changeSignKey, CancellationToken cancellationToken);
}