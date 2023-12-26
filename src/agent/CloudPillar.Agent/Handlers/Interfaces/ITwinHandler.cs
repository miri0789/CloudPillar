namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken, bool isInitial = false);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default);
    Task SaveLastTwinAsync(CancellationToken cancellationToken = default);

    public string GetLatestTwin();

}