

using Microsoft.AspNetCore.Mvc;
using Shared.Logger;

namespace CloudPillar.Agent.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentController : ControllerBase
{
    private readonly ILoggerHandler _logger;

    public AgentController(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("InitiateProvisioning")]
    public async Task<IActionResult> InitiateProvisioning()
    {
        return Ok();
    }

    [HttpPost("SetBusy")]
    public async Task<IActionResult> SetBusy()
    {
        return Ok();
    }


    [HttpPost("SetReady")]
    public async Task<IActionResult> SetReady()
    {
        return Ok();
    }

    [HttpPut("UpdateReportedProps")]
    public async Task<IActionResult> UpdateReportedProps()
    {
        return Ok();
    }

    [HttpGet("GetDeviceState")]
    public async Task<IActionResult> GetDeviceState()
    {
        _logger.Debug($"{nameof(GetDeviceState)} start");       
        return Ok();
    }


    [HttpPost("AddRecipe")]
    public async Task<IActionResult> AddRecipe()
    {
        return Ok();
    }
}

