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

    [HttpPost("SignData")]
    public async Task<IActionResult> CreateTwinKeySignature(byte[] dataToSign)
    {
        var signature = await _signingService.SignData(dataToSign);
        return Ok(signature);
    }

    [HttpPost("CreateFileSign")]
    public async Task<IActionResult> GetMeatadataFile(string deviceId, string propName, int actionIndex, byte[] data, string changeSpecKey)
    {
        await _signingService.CreateFileKeySignature(deviceId, propName, actionIndex, data, changeSpecKey);
        return Ok();
    }
}