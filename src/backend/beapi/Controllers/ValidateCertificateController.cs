using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.BEApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ValidateCertificateController : ControllerBase
{
    private readonly IValidateCertificateService _validateCertificateService;

    public ValidateCertificateController(IValidateCertificateService validateCertificateService)
    {
        _validateCertificateService = validateCertificateService ?? throw new ArgumentNullException(nameof(validateCertificateService));
    }

    [HttpGet("IsCertificateExpired/{deviceId}")]
    public async Task<bool> IsCertificateValid(string deviceId)
    {
        return await _validateCertificateService.IsCertificateExpiredAsync(deviceId);
    }
}