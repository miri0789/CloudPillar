using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Logger;

namespace Backend.BEApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DeviceCertificateController : ControllerBase
{
    private readonly ILoggerHandler _logger;
    private readonly IDeviceCertificateService _certificateService;
    private readonly ICertificateIdentityService _certificateIdentityService;
    public DeviceCertificateController(IDeviceCertificateService CertificateService, ICertificateIdentityService certificateIdentityService, ILoggerHandler logger)
    {
        _certificateService = CertificateService ?? throw new ArgumentNullException(nameof(CertificateService));
        _certificateIdentityService = certificateIdentityService ?? throw new ArgumentNullException(nameof(certificateIdentityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("IsDevicesCertificateExpired")]
    public async Task IsDevicesCertificateValid()
    {
        try
        {
            await _certificateService.IsDevicesCertificateExpiredAsync();
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
            await _certificateService.RemoveDeviceAsync(deviceId);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting device {deviceId}.", ex);
        }
    }

    [HttpGet("ProcessNewSigningCertificate")]
    public async Task<IActionResult> ProcessNewSigningCertificate(string deviceId)
    {
        try
        {
            await _certificateIdentityService.ProcessNewSigningCertificate(deviceId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.Info($"Error handling certificate: {ex.Message}");
            return BadRequest(ex.Message);

        }
    }
}