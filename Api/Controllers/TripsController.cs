using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Trips;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TripsController(AppDbContext context)
    {
        _context = context;
    }


     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Trip>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Trip> response = new();
        try
        {
            var query = _context.Trips.Include(x => x.Origin).ThenInclude(x => x!.Station).AsQueryable();

            if (request.Id.HasValue)
            {
                query = query.Where(x => x.Origin!.Id == request.Id);
            }
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    string pattern = $"%{request.SearchTerm}%";
                    query = query.Include(x => x.Truck).Include(x => x.Destination).ThenInclude(x => x!.Station)
                                .AsSplitQuery()
                                .Where(x => EF.Functions.ILike(x.WaybillNo!, pattern)
                                || EF.Functions.ILike(x.Origin!.Station!.Address!.State, pattern)
                                || EF.Functions.ILike(x.Origin!.Station!.Address!.Location, pattern)
                                || EF.Functions.ILike(x.Origin!.Station!.Address!.ContactAddress!, pattern)
                                || EF.Functions.ILike(x.Destination!.Station!.Address!.State, pattern)
                                || EF.Functions.ILike(x.Destination!.Station!.Address!.Location, pattern)
                                || EF.Functions.ILike(x.Destination!.Station!.Address!.ContactAddress!, pattern));
                }

            response.Total = await query.CountAsync();
            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Page).Take(request.PageSize).AsAsyncEnumerable();

            await foreach (var item in pagedQuery)
            {
                response.Data.Add(item);
            }


            return response;

            
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Trips
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Trip>>> GetTrips()
    {
        return await _context.Trips.ToListAsync();
    }

    // GET: api/Trips/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Trip>> GetTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);

        if (trip == null)
        {
            return NotFound();
        }

        return trip;
    }

    // PUT: api/Trips/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTrip(Guid id, Trip trip)
    {
        if (id != trip.Id)
        {
            return BadRequest();
        }

        _context.Entry(trip).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TripExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Trips
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Trip>> PostTrip(Trip trip)
    {
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTrip", new { id = trip.Id }, trip);
    }

    // DELETE: api/Trips/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TripExists(Guid id)
    {
        return _context.Trips.Any(e => e.Id == id);
    }
}
