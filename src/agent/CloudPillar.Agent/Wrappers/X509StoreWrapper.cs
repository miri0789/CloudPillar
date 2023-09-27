using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Wrappers;

public class X509StoreWrapper : IX509StoreWrapper
{

    X509Store IX509StoreWrapper.Create(StoreLocation storeLocation)
    {
        ArgumentNullException.ThrowIfNull(storeLocation);
        return new X509Store(storeLocation);
    }
}