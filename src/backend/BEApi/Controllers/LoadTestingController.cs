using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.BEApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LoadTestingController : ControllerBase
{
    private readonly ILoadTestingService _loadTestingService;
    public LoadTestingController(ILoadTestingService loadTestingService)
    {
        _loadTestingService = loadTestingService ?? throw new ArgumentNullException(nameof(loadTestingService));
    }

    [HttpGet("SendFileDownloadAsync")]
    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        try
        {
            await _loadTestingService.SendFileDownloadAsync(deviceId, data);
        }
        catch (Exception ex)
        {
            throw new Exception("Error validating certificates.", ex);
        }
    }
}