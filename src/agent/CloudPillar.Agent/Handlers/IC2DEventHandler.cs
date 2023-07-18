namespace CloudPillar.Agent.Interfaces;
public interface IC2DEventHandler
{
    Task CreateSubscribe(CancellationToken cancellationToken, string connectionString);
}
