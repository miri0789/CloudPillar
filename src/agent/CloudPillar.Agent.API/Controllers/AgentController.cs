

using CloudPillar.Agent.API.Entities;
using CloudPillar.Agent.API.Handlers;
using CloudPillar.Agent.API.Wrappers;
using FluentValidation;
using FluentValidation.Results;
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
    private readonly IValidator<UpdateReportedProps> _updateReportedPropsValidator;

    public AgentController(ITwinHandler twinHandler, IValidator<UpdateReportedProps> updateReportedPropsValidator, ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
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
    public async Task<IActionResult> UpdateReportedProps(UpdateReportedProps updateReportedProps)
    {       
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
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

