using Backend.BlobStreamer.Interfaces;
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

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMeatadata(string fileName)
    {
        var result = await _blobService.GetBlobMetadataAsync(fileName);
        return Ok(result);
    }

    [HttpPost("range")]
    public async Task<IActionResult> SendRange(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string actionId, string checkSum)
    {
        await _blobService.SendRangeByChunksAsync(deviceId, fileName, chunkSize, rangeSize, rangeIndex, startPosition, actionId, checkSum);
        return Ok();
    }

    [HttpPost("uploadStream")]
    public async Task UploadStream([FromBody] StreamingUploadChunkEvent data)
    {
        await _uploadStreamChunksService.UploadStreamChunkAsync(data.StorageUri, data.Data, data.StartPosition, data.CheckSum);
    }
}
