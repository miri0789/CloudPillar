namespace CloudPillar.Agent.Wrappers;

public interface IEnvironmentsWrapper
{
    string deviceConnectionString { get; }
    string transportType { get; }
}
