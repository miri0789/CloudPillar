namespace CloudPillar.Agent.Interfaces;

public interface IEnvironmentsWrapper
{
    string deviceConnectionString { get; }
    string transportType { get; }
}
