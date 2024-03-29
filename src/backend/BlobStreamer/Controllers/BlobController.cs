using Backend.BlobStreamer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Messages;

namespace Backend.BlobStreamer.Controllers;

[ApiController]
[Route("[controller]")]
public class BlobController : ControllerBase
{
    private readonly IBlobService _blobService;
    private readonly IUploadStreamChunksService _uploadStreamChunksService;

    public BlobController(IBlobService blobService, IUploadStreamChunksService uploadStreamChunksService)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _uploadStreamChunksService = uploadStreamChunksService ?? throw new ArgumentNullException(nameof(uploadStreamChunksService));
    }

    [HttpGet("Metadata")]
    public async Task<IActionResult> GetMeatadata(string fileName)
    {
        try
        {
            var result = await _blobService.GetBlobMetadataAsync(fileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("CalculateHash")]
    public async Task<IActionResult> CalculateHashtAsync(string deviceId, [FromBody] SignFileEvent signFileEvent)
    {
        var result = await _blobService.CalculateHashAsync(deviceId, signFileEvent);
        return Ok(result);
    }

    [HttpPost("Range")]
    public async Task<IActionResult> SendRange(string deviceId, string changeSpecId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, int actionIndex, int rangesCount)
    {
        var isSendRangeComplete = await _blobService.SendRangeByChunksAsync(deviceId, changeSpecId, fileName, chunkSize, rangeSize, rangeIndex, startPosition, actionIndex, rangesCount);
        return Ok(isSendRangeComplete);
    }

    [HttpPost("RangeError")]
    public async Task<IActionResult> SendRangeError(string deviceId, string changeSpecId, string fileName, int actionIndex, string error)
    {
        await _blobService.SendDownloadErrorAsync(deviceId, changeSpecId, fileName, actionIndex, error);
        return Ok();
    }

    [HttpPost("UploadStream")]
    public async Task UploadStream([FromBody] StreamingUploadChunkEvent data, string deviceId)
    {
        await _uploadStreamChunksService.UploadStreamChunkAsync(data.StorageUri, data.Data, data.StartPosition, data.CheckSum, data.FileName, deviceId, data.IsRunDiagnostics);
    }
}
