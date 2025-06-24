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
public class DestinationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public DestinationsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Destination>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Destination> response = new();
        try
        {
            var query = _context.TripDestinations.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Include(x => x.Station).Where(x => EF.Functions.ILike(x.Station!.Name, pattern) || EF.Functions.ILike(x.Station.Address!.Location, pattern)
                || EF.Functions.ILike(x.Station.Address!.State, pattern));
            }

            response.Total = await query.CountAsync();
            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

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

    // GET: api/Destinations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Destination>>> GetTripDestinations()
    {
        return await _context.TripDestinations.ToListAsync();
    }

    // GET: api/Destinations/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Destination>> GetDestination(Guid id)
    {
        var destination = await _context.TripDestinations.FindAsync(id);

        if (destination == null)
        {
            return NotFound();
        }

        return destination;
    }

    // PUT: api/Destinations/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDestination(Guid id, Destination destination)
    {
        if (id != destination.Id)
        {
            return BadRequest();
        }

        _context.Entry(destination).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DestinationExists(id))
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

    // POST: api/Destinations
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Destination>> PostDestination(Destination destination)
    {
        _context.TripDestinations.Add(destination);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetDestination", new { id = destination.Id }, destination);
    }

    // DELETE: api/Destinations/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDestination(Guid id)
    {
        var destination = await _context.TripDestinations.FindAsync(id);
        if (destination == null)
        {
            return NotFound();
        }

        _context.TripDestinations.Remove(destination);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DestinationExists(Guid id)
    {
        return _context.TripDestinations.Any(e => e.Id == id);
    }
}
