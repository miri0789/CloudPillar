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
    public async Task<IActionResult> SignData(byte[] dataToSign)
    {
        var signature = await _signingService.SignData(dataToSign);
        return Ok(signature);
    }
}