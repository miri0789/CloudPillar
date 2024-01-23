using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.BEApi.Controllers;

[ApiController]
[Route("[controller]")]
public class DeviceCertificateController : ControllerBase
{
    private readonly IDeviceCertificateService _certificateService;

    public DeviceCertificateController(IDeviceCertificateService CertificateService)
    {
        _certificateService = CertificateService ?? throw new ArgumentNullException(nameof(CertificateService));
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
}