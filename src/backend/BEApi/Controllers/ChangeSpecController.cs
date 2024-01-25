using Backend.BEApi.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Entities.Twin;

namespace BEApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChangeSpecController : ControllerBase
    {
        private readonly IChangeSpecService _changeSpecService;

        public ChangeSpecController(IChangeSpecService changeSpecService)
        {
            _changeSpecService = changeSpecService ?? throw new ArgumentNullException(nameof(changeSpecService));
        }

        [AllowAnonymous]
        [HttpPost("AssignChangeSpec")]
        public async Task<IActionResult> AssignChangeSpec([FromBody] AssignChangeSpec changeSpec)
        {
            await _changeSpecService.AssignChangeSpecAsync(changeSpec);

            return Ok();
        }
    }
}
