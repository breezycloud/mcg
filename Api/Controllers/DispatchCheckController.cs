using System.Text.RegularExpressions;
using Api.Context;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/dispatch")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
[EnableRateLimiting("DispatchCheckPolicy")]
public class DispatchCheckController : ControllerBase
{
    // Alphanumeric + dash only, 1–30 chars. Compiled for performance; timeout prevents ReDoS.
    private static readonly Regex DispatchIdRegex =
        new(@"^[a-zA-Z0-9\-]{1,30}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private readonly AppDbContext _context;
    private readonly ILogger<DispatchCheckController> _logger;

    public DispatchCheckController(AppDbContext context, ILogger<DispatchCheckController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/dispatch/check?dispatchId={dispatchId}
    /// Requires X-Api-Key header. Rate-limited to 20 requests per minute per IP.
    /// Returns { "exists": true/false } — no trip data is exposed.
    /// </summary>
    [HttpGet("check")]
    public async Task<IActionResult> CheckDispatch(
        [FromQuery] string? dispatchId,
        CancellationToken cancellationToken)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (string.IsNullOrWhiteSpace(dispatchId))
            return BadRequest(new { error = "dispatchId is required." });

        if (dispatchId.Length > 30 || !DispatchIdRegex.IsMatch(dispatchId))
            return BadRequest(new { error = "Invalid dispatchId format." });

        bool exists;
        try
        {
            exists = await _context.Trips.AnyAsync(
                t => t.DispatchId == dispatchId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DispatchCheck] DB error. IP={ClientIp}", clientIp);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred." });
        }

        // Log only the last 4 chars of the key — never log full keys
        string rawKey = HttpContext.Request.Headers["X-Api-Key"].ToString();
        string maskedKey = rawKey.Length >= 4
            ? new string('*', rawKey.Length - 4) + rawKey[^4..]
            : "****";

        _logger.LogInformation(
            "[DispatchCheck] IP={ClientIp} KeySuffix={MaskedKey} DispatchId={DispatchId} Exists={Exists} At={Timestamp}",
            clientIp, maskedKey, dispatchId, exists, DateTimeOffset.UtcNow);

        return Ok(new { exists });
    }
}
