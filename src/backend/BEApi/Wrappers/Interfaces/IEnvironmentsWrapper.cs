namespace Backend.BEApi.Wrappers.Interfaces;
public interface IEnvironmentsWrapper
{
    string iothubConnectionString { get; }
    string dpsConnectionString { get; }
    string dpsIdScope { get; }
    string globalDeviceEndpoint { get; }
}
