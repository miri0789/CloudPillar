
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
    private readonly ISymmetricKeyProvisioningDeviceClientHandler _symmetricKeyProvisioningDeviceClientHandler;

    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningDeviceClientHandler symmetricKeyProvisioningDeviceClientHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IC2DEventHandler c2DEventHandler,
     IFileUploaderHandler fileUploaderHandler,
     IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _symmetricKeyProvisioningDeviceClientHandler = symmetricKeyProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
    }

    [HttpPost("AddRecipe")]
    public async Task<IActionResult> AddRecipe()
    {
        return Ok();
    }

    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceState()
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioning(string registrationId = "pre-shread-key-enrollment", string primaryKey = "aUKETVe/YWlAxbYHAzLbyzR6rfLjWPOH4jYgs0XEOq/G9uwCijli/B25QldZcwp5zy1+TLO018RAf3lOvrRjHw==", CancellationToken cancellationToken = default)
    {
        try
        {
            var dpsScopeId = _environmentsWrapper.dpsScopeId;
            var globalDeviceEndpoint = _environmentsWrapper.globalDeviceEndpoint;

            var isAuthorized = await _symmetricKeyProvisioningDeviceClientHandler.AuthorizationAsync(CancellationToken.None);
            if (!isAuthorized)
            {
                try
                {
                    await _symmetricKeyProvisioningDeviceClientHandler.ProvisionWithSymmetricKeyAsync(registrationId, primaryKey, dpsScopeId, globalDeviceEndpoint, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Provisioning failed", ex);
                    return BadRequest("Provisioning failed");
                }
            }

            await _c2DEventHandler.CreateSubscribeAsync(cancellationToken);
            
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
}

