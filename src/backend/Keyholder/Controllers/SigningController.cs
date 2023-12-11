using Microsoft.AspNetCore.Mvc;
using Backend.Keyholder.Interfaces;

namespace Backend.Keyholder;


[ApiController]
[Route("[controller]")]
public class SigningController : ControllerBase
{
    private readonly ISigningService _signingService;
    public SigningController(ISigningService signingService)
    {
        _signingService = signingService;
    }

    [HttpGet("createTwinKeySignature")]
    public async Task<IActionResult> CreateTwinKeySignature(string deviceId)
    {
        await _signingService.CreateTwinKeySignature(deviceId);
        return Ok();
    }

    [HttpPost("createFileSign")]
    public async Task<IActionResult> GetMeatadataFile(string deviceId, string actionId, byte[] data)
    {
        await _signingService.CreateFileKeySignature(deviceId, actionId, data);
        return Ok();
    }
}


