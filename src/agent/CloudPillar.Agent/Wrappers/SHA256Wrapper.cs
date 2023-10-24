using System.Security.Cryptography;

namespace CloudPillar.Agent.Wrappers;
public class SHA256Wrapper : ISHA256Wrapper
{
   public byte[] ComputeHash(byte[] input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(input);
        }
    }
}