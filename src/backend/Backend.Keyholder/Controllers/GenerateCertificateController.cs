using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Shared.Logger;
using System.Text;

namespace Backend.Keyholder;

[ApiController]
[Route("[controller]")]
public class GenerateCertificateController : ControllerBase
{

    private readonly IRegistrationService _registrationService;
    private readonly ILoggerHandler _logger;


    public GenerateCertificateController(IRegistrationService registrationService, ILoggerHandler logger)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("GenerateCertificateForAgent")]

    public IActionResult GenerateCertificateForAgent(string deviceId, string OneMDKey, string iotHubHostName, string password)
    {

        _registrationService.Register(deviceId, OneMDKey, iotHubHostName, password);

        return Ok();
    }
}