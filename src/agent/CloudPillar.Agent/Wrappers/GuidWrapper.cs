namespace CloudPillar.Agent.Wrappers.interfaces;

public class GuidWrapper : IGuidWrapper
{
    public string CreateNewGUid()
    {
        return Guid.NewGuid().ToString();
    }

}