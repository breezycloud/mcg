using Microsoft.AspNetCore.Mvc;
using Shared.Helpers;

[ApiController]
[Route("api/[controller]")]
public class AppVersionController : ControllerBase
{
    // Written by the WriteVersionFile MSBuild target (Api.csproj) at publish time, so this
    // changes on every real deploy without anyone having to remember to bump a hardcoded
    // number. Read once at startup since it can't change while this process is running.
    private static readonly string AppVersion = ReadVersion();
    private static readonly DateTime ReleaseDate = ReadReleaseDate();

    private static string VersionFilePath => Path.Combine(AppContext.BaseDirectory, "version.txt");

    private static string ReadVersion() =>
        System.IO.File.Exists(VersionFilePath) ? System.IO.File.ReadAllText(VersionFilePath).Trim() : "dev";

    private static DateTime ReadReleaseDate() =>
        System.IO.File.Exists(VersionFilePath) ? System.IO.File.GetLastWriteTimeUtc(VersionFilePath) : DateTime.UtcNow;

    [HttpGet]
    public IActionResult GetVersionInfo()
    {
        return Ok(new VersionManifest
        {
            AppVersion = AppVersion,
            ForceUpdate = true,
            ReleaseDate = ReleaseDate,
            ReleaseNotes = "A new version of the app is available."
        });
    }
}