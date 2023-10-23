
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

    public readonly IStateMachineHandler _stateMachineHandler;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IStateMachineHandler StateMachineHandler,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _stateMachineHandler = StateMachineHandler ?? throw new ArgumentNullException(nameof(StateMachineHandler));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("AddRecipe")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> AddRecipe([FromBody] TwinDesired recipe)
    {
        _twinDesiredPropsValidator.ValidateAndThrow(recipe);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceState(CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = HttpContext.Request.Headers[Constants.X_DEVICE_ID].ToString();
        var secretKey = HttpContext.Request.Headers[Constants.X_SECRET_KEY].ToString();
        bool isX509Authorized = await _dPSProvisioningDeviceClientHandler.AuthorizationAsync(deviceId, secretKey, cancellationToken);
        if (!isX509Authorized)
        {
            var isSymetricKeyAuthorized = await _symmetricKeyProvisioningHandler.AuthorizationAsync(cancellationToken);
            if (!isSymetricKeyAuthorized)
            {
                await ProvisinigSymetricKey(cancellationToken);
            }
        }
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioning(CancellationToken cancellationToken)
    {
        try
        {
            await ProvisinigSymetricKey(cancellationToken);
            return await _twinHandler.GetTwinJsonAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("InitiateProvisioning failed ", ex);
            return BadRequest("An error occurred while processing the request.");
        }
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusy()
    {
        _stateMachineHandler.SetStateAsync(DeviceStateType.Busy);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("SetReadyAsync")]
    public async Task<ActionResult<string>> SetReadyAsync()
    {
        _stateMachineHandler.SetStateAsync(DeviceStateType.Ready);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPut("UpdateReportedProps")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> UpdateReportedProps([FromBody] UpdateReportedProps updateReportedProps)
    {
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
        return await _twinHandler.GetTwinJsonAsync();
    }

    private async Task ProvisinigSymetricKey(CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = HttpContext.Request.Headers[Constants.X_DEVICE_ID].ToString();
        var secretKey = HttpContext.Request.Headers[Constants.X_SECRET_KEY].ToString();
        await _symmetricKeyProvisioningHandler.ProvisioningAsync(deviceId, cancellationToken);
        _stateMachineHandler.SetStateAsync(DeviceStateType.Provisioning);
        await _twinHandler.UpdateDeviceSecretKeyAsync(secretKey);
    }
}

