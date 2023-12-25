using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Handlers;

public class PeriodicUploaderHandler : IPeriodicUploaderHandler
{
    private readonly ILoggerHandler _logger;

    public PeriodicUploaderHandler(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    
}
