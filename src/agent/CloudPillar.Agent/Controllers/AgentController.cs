
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
    private readonly IC2DEventHandler _c2DEventHandler;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;



    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IC2DEventHandler c2DEventHandler,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(C2DEventHandler));
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
    public async Task<ActionResult<string>> InitiateProvisioning(string dpsScopeId, string enrollmentId, string globalDeviceEndpoint, CancellationToken cancellationToken)
    {
        try
        {

            var cert = _dPSProvisioningDeviceClientHandler.GetCertificate();
            if (cert == null)
            {
                var error = "No certificate exist in agent";
                _logger.Error(error);
                return Unauthorized(error);
            }

            var isAuthorized = await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(cert, cancellationToken);
            if (!isAuthorized)
            {
                try
                {
                    await _dPSProvisioningDeviceClientHandler.ProvisioningAsync(dpsScopeId, cert, globalDeviceEndpoint, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Provisioning failed", ex);
                    return BadRequest("Provisioning failed");
                }
            }
            return await _twinHandler.GetTwinJsonAsync();
        }
        catch (Exception ex)
        {
            _logger.Error($"InitiateProvisioning error: ", ex);
            throw;
        }
    }

    [AllowAnonymous]
    [HttpPost("Message")]
    public async Task<ActionResult<string>> Message(CancellationToken cancellationToken)
    {
        await _c2DEventHandler.CreateSubscribeAsync(cancellationToken);
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

