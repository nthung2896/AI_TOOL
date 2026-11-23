using Hinet.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hinet.Controllers;

[ApiController, Authorize, Route("rental-media")]
public class RentalMediaController(RentalRequestRuntime runtime) : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Image(string id)
    {
        Response.Headers.CacheControl = "private, no-store";
        try { return PhysicalFile(Path.GetFullPath(runtime.Value.Media.Resolve(id)), RentalMedia.ContentType(id)); }
        catch (ArgumentException) { return NotFound(); }
    }
}
