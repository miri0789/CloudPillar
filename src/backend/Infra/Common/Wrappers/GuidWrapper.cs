using Backend.Infra.Common.Services.Interfaces;

namespace Backend.Infra.Common.Wrappers;

public class GuidWrapper : IGuidWrapper
{

    public GuidWrapper()
    {
    }

    public string NewGuid()
    {
        return Guid.NewGuid().ToString();
    }
}