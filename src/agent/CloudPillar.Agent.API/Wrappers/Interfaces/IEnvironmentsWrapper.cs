namespace CloudPillar.Agent.API.Wrappers;

public interface IEnvironmentsWrapper
{
    string deviceConnectionString { get; }
    string transportType { get; }
}
