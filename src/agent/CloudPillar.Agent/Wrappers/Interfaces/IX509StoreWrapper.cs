using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Wrappers;

public interface IX509StoreWrapper
{
    X509Store Create(StoreLocation storeLocation);
}