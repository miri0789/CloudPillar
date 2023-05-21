using blobstreamer.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace blobstreamer.Controllers;

[ApiController]
[Route("[controller]")]
public class BlobController : ControllerBase
{
    private readonly IBlobService _blobService;

    public BlobController(IBlobService blobService)
    {
        _blobService = blobService;
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMeatadata(string fileName)
    {
        var result = await _blobService.GetBlobMeatadataAsync(fileName);
        return Ok(result);
    }

    [HttpPost("range")]
    public async Task<IActionResult> SendRange(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition)
    {
        await _blobService.SendRangeByChunksAsync(deviceId, fileName, chunkSize, rangeSize, rangeIndex, startPosition);
        return Ok();
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(string deviceId, string fileName, long blobLength)
    {
        await _blobService.SendStartBlobMessage(deviceId, fileName, blobLength);
        return Ok();
    }
}
