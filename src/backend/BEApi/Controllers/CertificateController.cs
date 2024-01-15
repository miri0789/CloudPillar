using Backend.BEApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.BEApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CertificateController : ControllerBase
{
    private readonly IValidateCertificateService _validateCertificateService;
    private readonly ICertificateIdentityService _certificateIdentityService;

    public CertificateController(IValidateCertificateService validateCertificateService, ICertificateIdentityService certificateIdentityService)
    {
        _validateCertificateService = validateCertificateService ?? throw new ArgumentNullException(nameof(validateCertificateService));
        _certificateIdentityService = certificateIdentityService ?? throw new ArgumentNullException(nameof(certificateIdentityService));
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
}