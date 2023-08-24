namespace CloudPillar.Agent.API.Handlers;
public interface IC2DEventHandler
{
    Task CreateSubscribeAsync(CancellationToken cancellationToken);
}
