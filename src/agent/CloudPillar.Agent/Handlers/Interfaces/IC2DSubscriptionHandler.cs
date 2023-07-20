namespace CloudPillar.Agent.Handlers;
public interface IC2DSubscriptionHandler
{
    Task Subscribe(CancellationToken cancellationToken);
    void Unsubscribe();
    bool CheckSubscribed();
}
