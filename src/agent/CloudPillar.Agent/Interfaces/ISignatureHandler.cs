using System.Security.Cryptography;
using System.Text;

namespace CloudPillar.Agent.Interfaces;


public interface ISignatureHandler
{
    Task InitPublicKeyAsync();
    bool VerifySignature(string message, string signatureString);
}