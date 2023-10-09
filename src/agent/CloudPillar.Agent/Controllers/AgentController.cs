
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
    private readonly ISymmetricKeyWrapperDeviceClientHandler _symmetricKeyWrapperDeviceClientHandler;

private readonly IFileUploaderHandler _fileUploaderHandler;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyWrapperDeviceClientHandler symmetricKeyWrapperDeviceClientHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IFileUploaderHandler fileUploaderHandler,
     ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _symmetricKeyWrapperDeviceClientHandler = symmetricKeyWrapperDeviceClientHandler ?? throw new ArgumentNullException(nameof(symmetricKeyWrapperDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
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
    public async Task<ActionResult<string>> InitiateProvisioning(string dpsScopeId="0ne00B07A2A", string globalDeviceEndpoint="global.azure-devices-provisioning.net", string registrationId="pre-shread-key-enrollment", string primaryKey="aUKETVe/YWlAxbYHAzLbyzR6rfLjWPOH4jYgs0XEOq/G9uwCijli/B25QldZcwp5zy1+TLO018RAf3lOvrRjHw==")
    {
        try
        {
            var isAuthorized = await _symmetricKeyWrapperDeviceClientHandler.AuthorizationAsync(CancellationToken.None);
            if (!isAuthorized)
            {
                try
                {
                    await _symmetricKeyWrapperDeviceClientHandler.ProvisionWithSymmetricKeyAsync(registrationId, primaryKey, dpsScopeId, globalDeviceEndpoint);
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
    [HttpPost("Initiatex509Provisioning")]
    public async Task<ActionResult<string>> Initiatex509Provisioning(string dpsScopeId, string globalDeviceEndpoint, string registrationId, string primaryKey, CancellationToken cancellationToken)
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

