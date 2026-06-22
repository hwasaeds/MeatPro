using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeatPro.Controllers.Api;

[Authorize]
[ApiController]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected IActionResult OkOrNotFound<T>(T? model) where T : class
    {
        return model is null ? NotFound(new { error = "Resource not found." }) : Ok(model);
    }

    protected new IActionResult Created(string id, object value)
    {
        return CreatedAtAction(null, new { id }, value);
    }

    protected IActionResult Error(string message)
    {
        return BadRequest(new { error = message });
    }
}
