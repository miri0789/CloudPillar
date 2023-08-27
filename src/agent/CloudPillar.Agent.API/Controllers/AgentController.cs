

using CloudPillar.Agent.API.Handlers;
using CloudPillar.Agent.API.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;

namespace CloudPillar.Agent.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentController : ControllerBase
{
    private readonly ILoggerHandler _logger;
    private readonly ITwinHandler _twinHandler;

    public AgentController(ITwinHandler twinHandler, ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
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
    public async Task<ActionResult<string>> GetDeviceState()
    {       
       return await _twinHandler.GetTwinJsonAsync();
    }


    [HttpPost("AddRecipe")]
    public async Task<IActionResult> AddRecipe()
    {
        return Ok();
    }
}

