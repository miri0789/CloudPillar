namespace CloudPillar.Agent.Handlers;
public interface IC2DEventHandler
{
    Task CreateSubscribe(CancellationToken cancellationToken, string connectionString);
}
