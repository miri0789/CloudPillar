using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shared.Logger;
using System.Text;

namespace Backend.Keyholder;

[ApiController]
[Route("[controller]")]
public class RegisterByCertificateController : ControllerBase
{

    private readonly IRegistrationService _registrationService;
    private readonly ILoggerHandler _logger;


    public RegisterByCertificateController(IRegistrationService registrationService, ILoggerHandler logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register(string deviceId, string secretKey)
    {
        await _registrationService.RegisterAsync(deviceId, secretKey);
        return Ok();
    }

    [HttpPost("ProvisionDeviceCertificate")]
    public async Task<IActionResult> ProvisionDeviceCertificate(string deviceId, [FromBody] byte[] certificate)
    {
        await _registrationService.ProvisionDeviceCertificateAsync(deviceId, certificate);
        return Ok();
    }

}