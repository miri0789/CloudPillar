using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Shared.Logger;

namespace CloudPillar.Agent.API.Utilities;
public class LogActionFilter : IActionFilter
{
    private readonly ILoggerHandler _logger;

    public LogActionFilter(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Log information when an action is entered.
        _logger.Info($"Entering action '{context.ActionDescriptor.DisplayName}'");
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {        
    }
}