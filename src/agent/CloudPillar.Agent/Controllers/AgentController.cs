
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
    private readonly IC2DEventHandler _c2DEventHandler;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IC2DEventHandler c2DEventHandler,
     IFileUploaderHandler fileUploaderHandler,
     IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("AddRecipe")]
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
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReady()
    {
        await _twinHandler.HandleTwinActionsAsync(CancellationToken.None);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPut("UpdateReportedProps")]
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
        await _twinHandler.UpdateDeviceSecretKeyAsync(secretKey);
    }
}

