
using Backend.Infra.Common.Wrappers.Interfaces;

namespace Backend.Infra.Common.Wrappers;
public class EnvironmentsWrapper : IEnvironmentsWrapper
{
    private const string _iothubConnectionString = "IothubConnectionString";

    public string iothubConnectionString
    {
        get { return GetVariable(_iothubConnectionString); }
    }

    private string GetVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }
}
