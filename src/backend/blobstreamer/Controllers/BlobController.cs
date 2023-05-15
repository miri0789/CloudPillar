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
    public async Task<IActionResult> GetMeatadataAsync(string fileName)
    {
        var result = await _blobService.GetBlobMeatadataAsync(fileName);
        return Ok(result);
    }
}
