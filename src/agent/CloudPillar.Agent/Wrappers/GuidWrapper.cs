using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Wrappers;
public class GuidWrapper : IGuidWrapper
{
    public string CreateNewGuid()
    {
        return Guid.NewGuid().ToString();
    }
}
