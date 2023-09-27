using System.Runtime.InteropServices;
using CloudPillar.Agent.Wrappers;

public class RuntimeInformationWrapper : IRuntimeInformationWrapper
{
    public bool IsOSPlatform(OSPlatform platform)
    {
        return RuntimeInformation.IsOSPlatform(platform);
    }
    public string GetOSDescription()
    {
        return RuntimeInformation.OSDescription;
    }


}
