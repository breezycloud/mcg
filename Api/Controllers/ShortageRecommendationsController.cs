using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Trips;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ShortageRecommendationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShortageRecommendationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/ShortageRecommendations/trip/{tripId} — newest first, so the UI can just
    // take the first row as "current" and show the rest as history.
    [HttpGet("trip/{tripId}")]
    public async Task<ActionResult<IEnumerable<ShortageRecommendation>>> GetForTrip(Guid tripId)
    {
        return await _context.ShortageRecommendations
            .Where(x => x.TripId == tripId)
            .OrderByDescending(x => x.ReceivedDate)
            .ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Admin, Master, Supervisor, Manager")]
    public async Task<ActionResult<ShortageRecommendation>> Post(ShortageRecommendation recommendation)
    {
        if (!await _context.Trips.AnyAsync(t => t.Id == recommendation.TripId))
        {
            return BadRequest("Trip not found.");
        }

        recommendation.Id = Guid.NewGuid();
        recommendation.CreatedAt = DateTimeOffset.UtcNow;
        recommendation.RecordedById = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null;

        _context.ShortageRecommendations.Add(recommendation);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForTrip), new { tripId = recommendation.TripId }, recommendation);
    }
}
