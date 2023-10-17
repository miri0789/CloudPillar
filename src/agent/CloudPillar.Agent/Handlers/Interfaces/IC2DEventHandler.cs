namespace CloudPillar.Agent.Handlers;
public interface IC2DEventHandler
{
    void CreateSubscribe(CancellationToken cancellationToken, bool isProvisioning);
}
