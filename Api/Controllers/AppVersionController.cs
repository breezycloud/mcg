using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class AppVersionController : ControllerBase
{
    // [Authorize]
    private double AppVersion;
    [HttpGet]
    public IActionResult GetVersionInfo()
    {
        #if DEBUG
            AppVersion = 1.1;
        #else
            AppVersion = 1.2;
        #endif
        return Ok(new VersionManifest
        {
            AppVersion = AppVersion, // Update this with each release
            ForceUpdate = true,    // Set to true when critical updates are available
            ReleaseDate = DateTime.Now,
            ReleaseNotes = "Bug fixes"
        });
    }
}