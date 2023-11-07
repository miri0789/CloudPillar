
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
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
    private readonly IStrictModeHandler _strictModeHandler;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IStateMachineHandler stateMachineHandler,
     IStrictModeHandler strictModeHandler,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(StateMachineHandler));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("AddRecipe")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> AddRecipeAsync([FromBody] TwinDesired recipe)
    {
        _twinDesiredPropsValidator.ValidateAndThrow(recipe);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceStateAsync(CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = HttpContext.Request.Headers[Constants.X_DEVICE_ID].ToString();
        var secretKey = HttpContext.Request.Headers[Constants.X_SECRET_KEY].ToString();
        bool isX509Authorized = await _dPSProvisioningDeviceClientHandler.AuthorizationDeviceAsync(deviceId, secretKey, cancellationToken);
        if (!isX509Authorized)
        {
            _logger.Info("GetDeviceStateAsync, the device is X509 unAuthorized, check  symmetric key authorized");
            var isSymetricKeyAuthorized = await _symmetricKeyProvisioningHandler.AuthorizationDeviceAsync(cancellationToken);
            if (!isSymetricKeyAuthorized)
            {
                _logger.Info("GetDeviceStateAsync, the device is symmetric key unAuthorized, start provisinig proccess");
                await ProvisinigSymetricKeyAsync(cancellationToken);
            }
        }
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioningAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ProvisinigSymetricKeyAsync(cancellationToken);
            return await _twinHandler.GetTwinJsonAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("InitiateProvisioning failed ", ex);
            return BadRequest("An error occurred while processing the request.");
        }
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusyAsync()
    {
        _stateMachineHandler.SetStateAsync(DeviceStateType.Busy);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReadyAsync()
    {
        _twinHandler.OnDesiredPropertiesUpdate(CancellationToken.None);

        _stateMachineHandler.SetStateAsync(DeviceStateType.Ready);
        return await _twinHandler.GetTwinJsonAsync();
    }
    [HttpPost("TestStrictMode")]
    public async Task<ActionResult<string>> TestStrictMode(string root,string pattern)
    {
        try
        {
            Matcher matcher = new Matcher();

            matcher.AddIncludePatterns(new[] { pattern});

            var inMemoryFileNames = new List<string>
        {
            "c:/demo1/test.txt",
            "c:/demo1/file2.log",
            "c:/demo1/file2.log",
            "c:/test.txt",
            "D:/dd/dir1/file3.log",
            "c:/demo/dir2/file4.md",
            "c:/demo/dir2/subdir/file5.cs"
        };

            var result = matcher.Match(root, inMemoryFileNames);

            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPut("UpdateReportedProps")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> UpdateReportedPropsAsync([FromBody] UpdateReportedProps updateReportedProps)
    {
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
        return await _twinHandler.GetTwinJsonAsync();
    }

    private async Task ProvisinigSymetricKeyAsync(CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = HttpContext.Request.Headers[Constants.X_DEVICE_ID].ToString();
        var secretKey = HttpContext.Request.Headers[Constants.X_SECRET_KEY].ToString();
        await _symmetricKeyProvisioningHandler.ProvisioningAsync(deviceId, cancellationToken);
        _stateMachineHandler.SetStateAsync(DeviceStateType.Provisioning);
        await _twinHandler.UpdateDeviceSecretKeyAsync(secretKey);
    }
}

