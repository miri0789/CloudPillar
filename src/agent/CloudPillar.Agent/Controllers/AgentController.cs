
using System.Diagnostics;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;
using CloudPillar.Agent.Sevices.Interfaces;

namespace CloudPillar.Agent.Controllers;

[ApiController]
[Route("[controller]")]
public class AgentController : ControllerBase
{
    private readonly ILoggerHandler _logger;

    private readonly ITwinHandler _twinHandler;
    private readonly ITwinReportHandler _twinReportHandler;
    public readonly IStateMachineHandler _stateMachineHandler;
    private readonly IDPSProvisioningDeviceClientHandler _dPSProvisioningDeviceClientHandler;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;
    private readonly IRunDiagnosticsHandler _runDiagnosticsHandler;
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly IReprovisioningHandler _reprovisioningHandler;
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private readonly IRequestWrapper _requestWrapper;
    private readonly IProvisioningService _provisioningService;

    public AgentController(ITwinHandler twinHandler,
     ITwinReportHandler twinReportHandler,
     IDPSProvisioningDeviceClientHandler dPSProvisioningDeviceClientHandler,
     ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
     IStateMachineHandler stateMachineHandler,
     IRunDiagnosticsHandler runDiagnosticsHandler,
     ILoggerHandler logger,
     IStateMachineChangedEvent stateMachineChangedEvent,
     IReprovisioningHandler reprovisioningHandler,
    IServerIdentityHandler serverIdentityHandler,
    IRequestWrapper requestWrapper,
    IProvisioningService provisioningService)
    {
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
        _dPSProvisioningDeviceClientHandler = dPSProvisioningDeviceClientHandler ?? throw new ArgumentNullException(nameof(dPSProvisioningDeviceClientHandler));
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(StateMachineHandler));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _runDiagnosticsHandler = runDiagnosticsHandler ?? throw new ArgumentNullException(nameof(runDiagnosticsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _reprovisioningHandler = reprovisioningHandler ?? throw new ArgumentNullException(nameof(reprovisioningHandler));
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
        _requestWrapper = requestWrapper ?? throw new ArgumentNullException(nameof(requestWrapper));
        _provisioningService = provisioningService ?? throw new ArgumentNullException(nameof(provisioningService));
    }

    [HttpPost("AddRecipe")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> AddRecipeAsync([FromBody] TwinDesired recipe)
    {
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpGet("GetDeviceState")]
    public async Task<ActionResult<string>> GetDeviceStateAsync(CancellationToken cancellationToken)
    {
        var currentState = _stateMachineHandler.GetCurrentDeviceState();
        if (currentState == DeviceStateType.Busy)
        {
            return _twinHandler.GetLatestTwin();
        }
        return await _twinHandler.GetTwinJsonAsync();
    }

    [AllowAnonymous]
    [HttpPost("InitiateProvisioning")]
    public async Task<ActionResult<string>> InitiateProvisioningAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _provisioningService.ProvisinigSymetricKeyAsync(cancellationToken);
            return await _twinHandler.GetTwinJsonAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("InitiateProvisioning failed ", ex);
            return BadRequest($"An error occurred while processing the request: {ex.Message}");
        }
    }

    [HttpPost("SetBusy")]
    public async Task<ActionResult<string>> SetBusyAsync(CancellationToken cancellationToken)
    {
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Busy, cancellationToken);
        return _twinHandler.GetLatestTwin();
    }

    [HttpPost("SetReady")]
    public async Task<ActionResult<string>> SetReadyAsync(CancellationToken cancellationToken)
    {
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Ready, cancellationToken);
        return await _twinHandler.GetTwinJsonAsync();
    }

    [HttpPut("UpdateReportedProps")]
    [DeviceStateFilter]
    public async Task<ActionResult<string>> UpdateReportedPropsAsync([FromBody] UpdateReportedProps updateReportedProps, CancellationToken cancellationToken)
    {
        await _twinReportHandler.UpdateDeviceCustomPropsAsync(updateReportedProps.Properties, cancellationToken);
        return await _twinHandler.GetTwinJsonAsync(cancellationToken);
    }

    [HttpGet("RunDiagnostics")]
    public async Task<ActionResult<string>> RunDiagnostics()
    {
        if (RunDiagnosticsHandler.IsDiagnosticsProcessRunning)
        {
            var message = "RunDiagnostics is already running";
            _logger.Info(message);
            return BadRequest(message);
        }
        try
        {
            RunDiagnosticsHandler.IsDiagnosticsProcessRunning = true;
            Stopwatch timeTaken = new Stopwatch();
            timeTaken.Start();

            var reported = await _runDiagnosticsHandler.HandleRunDiagnosticsProcess(CancellationToken.None);

            timeTaken.Stop();

            if (reported.Status == StatusType.Success)
            {
                var timeTakenString = timeTaken.Elapsed.ToString(@"mm\:ss");
                _logger.Info($"RunDiagnostics Success in {timeTakenString}");
                return Ok($"The diagnostic process has been completed successfully, request-duration: {timeTakenString}");
            }
            else
            {
                throw new Exception(reported.ResultText);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("RunDiagnostics failed", ex);
            return BadRequest($"An error occurred while processing run diagnostics: {ex.Message}");
        }
        finally
        {
            RunDiagnosticsHandler.IsDiagnosticsProcessRunning = false;
        }
    }
}

