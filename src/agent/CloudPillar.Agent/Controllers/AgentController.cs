
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using FluentValidation;
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
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly IValidator<UpdateReportedProps> _updateReportedPropsValidator;

    public AgentController(ITwinHandler twinHandler,IFileUploaderHandler fileUploaderHandler, IValidator<UpdateReportedProps> updateReportedPropsValidator, ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _updateReportedPropsValidator = updateReportedPropsValidator ?? throw new ArgumentNullException(nameof(updateReportedPropsValidator));
    }
    [HttpGet("TwinHandler")]
    public async Task<IActionResult> TwinHandler(string fileName = "C:\\demo\\stream.jpg")
    {
        await _twinHandler.HandleTwinActionsAsync(CancellationToken.None);

        UploadAction uploadAction = new UploadAction()
        {
            Action = TwinActionType.SingularUpload,
            Description = "test upload by stream",
            Enabled = true,
            Method = FileUploadMethod.Stream,
            FileName = fileName
        };
        ActionToReport actionToReport = new ActionToReport()
        {
            TwinAction = uploadAction,
            TwinReport = new TwinActionReported(),
        };

        var twinReport = await _fileUploaderHandler.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);
        if (twinReport.TwinReport.Status == StatusType.Success)
        {

        }

        await _twinHandler.HandleTwinActionsAsync(CancellationToken.None);
        return Ok();
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

    [HttpPost("InitiateProvisioning")]
    public async Task<IActionResult> InitiateProvisioning()
    {
        return Ok();
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

