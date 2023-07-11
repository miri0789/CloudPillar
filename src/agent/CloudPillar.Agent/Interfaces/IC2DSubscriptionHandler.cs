namespace CloudPillar.Agent.Interfaces;
public interface IC2DSubscriptionHandler
{
    Task Subscribe(CancellationToken cancellationToken);
    void Unsubscribe();
    bool CheckSubscribed();
}
