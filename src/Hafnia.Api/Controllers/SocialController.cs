using Microsoft.AspNetCore.Mvc;

namespace Hafnia.Api.Controllers;

[ApiController]
[Route("social")]
public class SocialController : ControllerBase
{
    public SocialController()
    {

    }

    [HttpGet("messages")]
    public Task<IActionResult> GetMessages(CancellationToken cancellationToken)
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}
