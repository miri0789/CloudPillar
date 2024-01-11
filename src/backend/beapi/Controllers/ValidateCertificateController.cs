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

    [HttpGet("IsCertificateExpired")]
    public async Task IsCertificateValid()
    {
        try
        {
            await _validateCertificateService.IsDevicesCertificateExpiredAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error validating certificates.", ex);
        }
    }

    [HttpDelete("RemoveDevice/{deviceId}")]
    public async Task RemoveDevice(string deviceId)
    {
        try
        {
            await _validateCertificateService.RemoveDeviceAsync(deviceId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting device {deviceId}.", ex);
        }
    }
}