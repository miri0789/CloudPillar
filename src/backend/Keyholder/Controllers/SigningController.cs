using Microsoft.AspNetCore.Mvc;
using Backend.Keyholder.Interfaces;
using System.Text;

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

    [HttpPost("createFileSign")]
    public async Task<IActionResult> GetMeatadataFile(string deviceId, string propName, int actionIndex, byte[] data, string changeSpecKey)
    {
        await _signingService.CreateFileKeySignature(deviceId, propName, actionIndex, data, changeSpecKey);
        return Ok();
    }

    [HttpPost("signData")]
    public async Task<IActionResult> SignData(string deviceId, [FromBody] byte[] data)
    {
        var sign = await _signingService.SignData(data, deviceId);
        byte[] signBytes = Encoding.UTF8.GetBytes(sign);
        return Ok(signBytes);
    }


    [HttpGet("GetSigningPublicKeyAsync")]
    public async Task<IActionResult> GetSigningPublicKeyAsync()
    {
        var publicKey = await _signingService.GetSigningPublicKeyAsync();
        return Ok(publicKey);
    }
}


