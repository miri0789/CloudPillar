using Backend.BlobStreamer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Events;
using Shared.Logger;

namespace Backend.BlobStreamer.Controllers;

[ApiController]
[Route("[controller]")]
public class BlobController : ControllerBase
{
    private readonly IBlobService _blobService;

    public BlobController(IBlobService blobService)
    {
        _blobService = blobService;
    }
    [HttpGet("test")]
    public async Task<IActionResult> Test(string fileName)
    {
        return Ok(fileName);
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMeatadata(string fileName)
    {
        var result = await _blobService.GetBlobMetadataAsync(fileName);
        return Ok(result);
    }

    [HttpPost("range")]
    public async Task<IActionResult> SendRange(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string actionId, long fileSize)
    {
        await _blobService.SendRangeByChunksAsync(deviceId, fileName, chunkSize, rangeSize, rangeIndex, startPosition, actionId, fileSize);
        return Ok();
    }
    [HttpPost("uploadStream")]
    public async Task<IActionResult> UploadStream([FromBody] StreamingUploadChunkEvent data)
    {
        await _blobService.UploadFromStreamAsync(new Uri(data.AbsolutePath), data.Data);
        return Ok();
    }
}
