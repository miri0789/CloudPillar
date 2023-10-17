namespace CloudPillar.Agent.Handlers;
public interface IC2DEventHandler
{
    void CreateSubscribeAsync(CancellationToken cancellationToken, bool isProvisioning);
}
