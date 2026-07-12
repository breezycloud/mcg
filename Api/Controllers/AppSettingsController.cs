using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Settings;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Master, Admin")]
public class AppSettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AppSettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationSettings>> Get(CancellationToken cancellationToken)
    {
        var settings = await _context.AppSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = new NotificationSettings { Id = Guid.NewGuid() };
            _context.AppSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    [HttpPut]
    public async Task<IActionResult> Update(NotificationSettings settings, CancellationToken cancellationToken)
    {
        var existing = await _context.AppSettings.FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            settings.Id = settings.Id == Guid.Empty ? Guid.NewGuid() : settings.Id;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            _context.AppSettings.Add(settings);
        }
        else
        {
            existing.NrlCcuEmail = settings.NrlCcuEmail;
            existing.NrlCcuCcEmails = settings.NrlCcuCcEmails;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
