using CloudPillar.Agent.Handlers.Logger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CloudPillar.Agent.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
public class ErrorController : ControllerBase
{
    private readonly ILoggerHandler _logger;
    public ErrorController(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Route("/error")]
    public IActionResult Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        _logger.Error($"Error handler api exception type: {context.Error.GetType().Name} message: {context.Error.Message}");
        _logger.Debug("Error handler api exception StackTrace: ", context.Error.StackTrace);

        return Problem(
            statusCode: (int)HttpStatusCode.InternalServerError,
            title: context.Error.Message);
    }
}
