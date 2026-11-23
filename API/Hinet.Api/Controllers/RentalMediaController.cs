using Hinet.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hinet.Controllers;

[ApiController, AllowAnonymous, Route("rental-media")]
public class RentalMediaController(RentalMedia media) : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Image(string id)
    {
        try { return PhysicalFile(Path.GetFullPath(media.Resolve(id)), RentalMedia.ContentType(id)); }
        catch (ArgumentException) { return NotFound(); }
    }
}
