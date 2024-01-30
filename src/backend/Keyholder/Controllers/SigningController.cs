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
    public async Task<IActionResult> CreateTwinKeySignature(string deviceId, string changeSignKey)
    {
        await _signingService.CreateTwinKeySignature(deviceId, changeSignKey);
        return Ok();
    }

    [HttpPost("createFileSign")]
    public async Task<IActionResult> GetMeatadataFile(string deviceId, string propName, int actionIndex, byte[] data, string changeSpecKey)
    {
        await _signingService.CreateFileKeySignature(deviceId, propName, actionIndex, data, changeSpecKey);
        return Ok();
    }

    [HttpGet("GetPublicKey")]
    public async Task<IActionResult> GetPublicKey()
    {
        var publicKey = await _signingService.GetSigningPublicKeyAsync();
        return Ok(publicKey);
    }
}


