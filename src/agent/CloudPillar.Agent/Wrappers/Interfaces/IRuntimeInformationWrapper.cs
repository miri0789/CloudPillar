using System.Runtime.InteropServices;

namespace CloudPillar.Agent.Wrappers;

public interface  IRuntimeInformationWrapper
{
    bool IsOSPlatform(OSPlatform platform);
    string GetOSDescription();


}