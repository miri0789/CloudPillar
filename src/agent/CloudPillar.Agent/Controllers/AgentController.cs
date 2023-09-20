
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentController : ControllerBase
{
    private readonly ITwinHandler _twinHandler;
    private readonly IValidator<UpdateReportedProps> _updateReportedPropsValidator;
    private readonly IValidator<TwinDesired> _twinDesiredPropsValidator;

    public AgentController(ITwinHandler twinHandler,
                            IValidator<UpdateReportedProps> updateReportedPropsValidator,
                            IValidator<TwinDesired> twinDesiredPropsValidator
                            )
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
    }
   
    [HttpPost("AddRecipe")]
    public async Task<ActionResult<string>> AddRecipe(TwinDesired recipe)
    {
        _twinDesiredPropsValidator.ValidateAndThrow(recipe);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceState()
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioning()
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusy()
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReady()
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPut("UpdateReportedProps")]
    public async Task<ActionResult<string>> UpdateReportedProps(UpdateReportedProps updateReportedProps)
    {
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
        return await _twinHandler.GetTwinJsonAsync();
    }
}