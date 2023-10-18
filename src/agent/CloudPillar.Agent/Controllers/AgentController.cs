
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Provisioning.Service;
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
    private readonly IStateMachine _stateMachine;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
    IStateMachine stateMachine,
     IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("AddRecipe")]
    public async Task<ActionResult<string>> AddRecipe(TwinDesired recipe)
    {
        _twinDesiredPropsValidator.ValidateAndThrow(recipe);
        return (await _twinHandler.GetTwinJsonAsync())?.ToJson();
    }


    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceState()
    {
        return (await _twinHandler.GetTwinJsonAsync())?.ToJson();
    }

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioning(string registrationId, string primaryKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var dpsScopeId = _environmentsWrapper.dpsScopeId;
            var globalDeviceEndpoint = _environmentsWrapper.globalDeviceEndpoint;

            var isAuthorized = await _symmetricKeyProvisioningHandler.AuthorizationAsync(cancellationToken);
            if (!isAuthorized)
            {
                try
                {
                    await _symmetricKeyProvisioningHandler.ProvisioningAsync(registrationId, primaryKey, dpsScopeId, globalDeviceEndpoint, cancellationToken);
                    await _stateMachine.SetState(DeviceStateType.Provisioning);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Provisioning failed", ex);
                    return BadRequest("Provisioning failed");
                }
            }
            else
            {
                await _stateMachine.SetState(DeviceStateType.Ready);
            }


            return (await _twinHandler.GetTwinJsonAsync())?.ToJson();
        }
        catch (Exception ex)
        {
            _logger.Error($"InitiateProvisioning error: ", ex);
            throw;
        }
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusy()
    {
        var twin = await _twinHandler.GetTwinJsonAsync();
        _stateMachine.SetState(DeviceStateType.Busy);
        return twin?.ToJson();
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReady()
    {
        _stateMachine.SetState(DeviceStateType.Ready);
        return (await _twinHandler.GetTwinJsonAsync())?.ToJson();
    }

    [HttpPut("UpdateReportedProps")]
    public async Task<ActionResult<string>> UpdateReportedProps(UpdateReportedProps updateReportedProps)
    {
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
        return (await _twinHandler.GetTwinJsonAsync())?.ToJson();
    }
}

