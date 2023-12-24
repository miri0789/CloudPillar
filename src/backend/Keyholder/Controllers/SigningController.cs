using Microsoft.AspNetCore.Mvc;
using Backend.Keyholder.Interfaces;
using System.Runtime.ConstrainedExecution;

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
    public async Task<IActionResult> GetMeatadataFile(string deviceId, string propName, int actionIndex, byte[] data, TwinPatchChangeSpec changeSpecKey)
    {
        await _signingService.CreateFileKeySignature(deviceId, propName, actionIndex, data, changeSpecKey);
        return Ok();
    }
}


