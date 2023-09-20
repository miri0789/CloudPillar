
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentController : ControllerBase
{
    private readonly ILoggerHandler _logger;

    private readonly ITwinHandler _twinHandler;

    private readonly IValidator<UpdateReportedProps> _updateReportedPropsValidator;
    private readonly IValidator<TwinDesired> _twinDesiredPropsValidator;

    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;



    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
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

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioning(string dpsScopeId, string globalDeviceEndpoint)
    {
        try
        {

            var cert = _dPSProvisioningDeviceClientHandler.GetCertificate();
            if (cert == null)
            {
                var error  = "No certificate exist in agent";
                _logger.Error(error);
                return Unauthorized(error);
            }

            var isAuthorized = await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(cert);
            if (!isAuthorized)
            {
                if (await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(dpsScopeId, cert, globalDeviceEndpoint))
                {
                    await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(cert);
                }
                else
                {
                    return BadRequest("Failed to provisioning");
                }
            }
            return await _twinHandler.GetTwinJsonAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"InitiateProvisioning error: {ex.Message}");
            throw;
        }
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
