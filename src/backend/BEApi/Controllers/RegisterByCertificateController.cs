using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Logger;

namespace Backend.BEApi.Controllers;

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
    public async Task<IActionResult> RegisterAsync(string deviceId, string secretKey)
    {
        await _registrationService.RegisterAsync(deviceId, secretKey);
        return Ok();
    }

    [HttpPost("RegisterByOneMDReport")]
    public async Task<IActionResult> RegisterByOneMDReport(string deviceId, string secretKey)
    {
        await _registrationService.RegisterByOneMDReportAsync(deviceId, secretKey);
        return Ok();
    }

    [HttpPost("ProvisionDeviceCertificate")]
    public async Task<IActionResult> ProvisionDeviceCertificateAsync(string deviceId, string prefix, [FromBody] byte[] certificate)
    {
        await _registrationService.ProvisionDeviceCertificateAsync(deviceId, prefix, certificate);
        return Ok();
    }
}