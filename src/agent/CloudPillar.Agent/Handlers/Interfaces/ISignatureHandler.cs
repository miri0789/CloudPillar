using System.Security.Cryptography;
using System.Text;

namespace CloudPillar.Agent.Handlers;


public interface ISignatureHandler
{
    Task InitPublicKeyAsync();
    Task<bool> VerifySignatureAsync(string message, string signatureString);
    Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey);
}