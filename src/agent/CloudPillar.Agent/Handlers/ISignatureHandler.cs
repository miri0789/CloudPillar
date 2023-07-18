using System.Security.Cryptography;
using System.Text;

namespace CloudPillar.Agent.Handlers;


public interface ISignatureHandler
{
    Task InitPublicKeyAsync();
    bool VerifySignature(string message, string signatureString);
}