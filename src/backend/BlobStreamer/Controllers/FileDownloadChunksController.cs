using Backend.BlobStreamer.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Messages;

namespace Backend.BlobStreamer.Controllers;

[ApiController]
[Route("[controller]")]
public class FileDownloadChunksController : ControllerBase
{
    private readonly IFileDownloadChunksService _fileDownloadsService;

    public FileDownloadChunksController(IFileDownloadChunksService fileDownloadsService)
    {
        _fileDownloadsService = fileDownloadsService ?? throw new ArgumentNullException(nameof(fileDownloadsService));
    }

    [HttpPost("SendFileDownloadChunks")]
    public async Task<IActionResult> SendFileDownload(string deviceId, [FromBody] FileDownloadEvent data)
    {
        await _fileDownloadsService.SendFileDownloadAsync(deviceId, data);
        return Ok();
    }
}