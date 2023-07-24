namespace CloudPillar.Agent.Handlers;
public interface IC2DEventHandler
{
    Task CreateSubscribeAsync(CancellationToken cancellationToken, string connectionString);
}
