using Microsoft.AspNetCore.Mvc;

namespace keyholder;


[ApiController]
[Route("[controller]")]
public class SigningController : ControllerBase
{
    private readonly ISigningService _signingService;
    public SigningController(ISigningService signingService)
    {
        _signingService = signingService;
    }

    [HttpGet("create")]
    public async Task<IActionResult> GetMeatadata(string deviceId, string keyPath, string signatureKey)
    {
        await _signingService.CreateTwinKeySignature(deviceId, keyPath, signatureKey);
        return Ok();
    }
}


    