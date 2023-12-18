using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Wrappers;

public class GuidWrapper : IGuidWrapper
{
    public string CreateNewGUid()
    {
        return Guid.NewGuid().ToString();
    }

}