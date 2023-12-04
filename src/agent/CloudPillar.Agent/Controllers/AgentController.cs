
using System.Diagnostics;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
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
    private readonly IRunDiagnosticsHandler _runDiagnosticsHandler;


    public AgentController(ITwinHandler twinHandler,
     IValidator<UpdateReportedProps> updateReportedPropsValidator,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IValidator<TwinDesired> twinDesiredPropsValidator,
     IStateMachineHandler stateMachineHandler,
     IRunDiagnosticsHandler runDiagnosticsHandler,
     ILoggerHandler logger)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _twinDesiredPropsValidator = twinDesiredPropsValidator ?? throw new ArgumentNullException(nameof(twinDesiredPropsValidator));
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(StateMachineHandler));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _runDiagnosticsHandler = runDiagnosticsHandler ?? throw new ArgumentNullException(nameof(runDiagnosticsHandler));
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
        var currentState = _stateMachineHandler.GetCurrentDeviceState();
        if (currentState == DeviceStateType.Busy)
        {
            return await _twinHandler.GetLatestTwinAsync();
        }

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
            return BadRequest($"An error occurred while processing the request: {ex.Message}");
        }
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusyAsync()
    {
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Busy);
        return await _twinHandler.GetLatestTwinAsync(CancellationToken.None);
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReadyAsync()
    {
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Ready);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPut("UpdateReportedProps")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> UpdateReportedPropsAsync([FromBody] UpdateReportedProps updateReportedProps, CancellationToken cancellationToken)
    {
        _updateReportedPropsValidator.ValidateAndThrow(updateReportedProps);
        await _twinHandler.UpdateDeviceCustomPropsAsync(updateReportedProps.Properties, cancellationToken);
        return await _twinHandler.GetTwinJsonAsync(cancellationToken);
    }

    [HttpGet("RunDiagnostics")]
    public async Task<ActionResult<string>> RunDiagnostics()
    {
        try
        {
            Stopwatch timeTaken = new Stopwatch();
            timeTaken.Start();

            var filePath = await _runDiagnosticsHandler.CreateFileAsync();
            var actionId = await _runDiagnosticsHandler.UploadFileAsync(filePath, CancellationToken.None);
            var reported = await _runDiagnosticsHandler.CheckDownloadStatus(actionId, filePath);

            timeTaken.Stop();
            if (reported.Status == StatusType.Success)
            {
                await _runDiagnosticsHandler.DeleteFileAsync(filePath, CancellationToken.None);
                var timeTakenString = timeTaken.Elapsed.ToString(@"mm\:ss");
                _logger.Info($"RunDiagnostics Success in {timeTakenString}");
                return Ok($"The diagnostic process has been completed successfully, request-duration: {timeTakenString}");
            }
            else
            {
                return BadRequest($"An error occurred while processing run diagnostics: {reported.ResultText}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("RunDiagnostics failed", ex);
            return BadRequest($"An error occurred while processing run diagnostics: {ex.Message}");
        }
    }

    private async Task ProvisinigSymetricKeyAsync(CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = HttpContext.Request.Headers[Constants.X_DEVICE_ID].ToString();
        var secretKey = HttpContext.Request.Headers[Constants.X_SECRET_KEY].ToString();
        await _symmetricKeyProvisioningHandler.ProvisioningAsync(deviceId, cancellationToken);
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Provisioning);
        await _twinHandler.UpdateDeviceSecretKeyAsync(secretKey);
    }
}

