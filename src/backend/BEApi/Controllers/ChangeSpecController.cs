using Backend.BEApi.Services.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BEApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChangeSpecController : ControllerBase
    {
        private readonly IChangeSpecService _changeSpecService;

        public ChangeSpecController(IChangeSpecService changeSpecService)
        {
            _changeSpecService = changeSpecService ?? throw new ArgumentNullException(nameof(changeSpecService));
        }

        [HttpPost("AssignChangeSpec")]
        public async Task<IActionResult> AssignChangeSpec(string devices, string changeSpecKey, [FromBody] dynamic assignChangeSpec)
        {
            await _changeSpecService.AssignChangeSpecAsync(assignChangeSpec, devices, changeSpecKey);
            return Ok();
        }

        [HttpPost("CreateChangeSpecKeySignature")]
        public async Task<IActionResult> CreateChangeSpecKeySignature(string deviceId, string changeSignKey)
        {
            await _changeSpecService.CreateChangeSpecKeySignatureAsync(deviceId, changeSignKey);
            return Ok();
        }

        [HttpPost("CreateFileSign")]
        public async Task<IActionResult> CreateFileSign(string deviceId, string propName, int actionIndex, string changeSpecKey, [FromBody] string signatureFileBytes)
        {
            await _changeSpecService.CreateFileKeySignatureAsync(deviceId, propName, actionIndex, changeSpecKey, signatureFileBytes);
            return Ok();
        }
    }
}