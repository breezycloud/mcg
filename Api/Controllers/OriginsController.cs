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
public class OriginsController : ControllerBase
{
    private readonly AppDbContext _context;

    public OriginsController(AppDbContext context)
    {
        _context = context;
    }


    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Origin>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Origin> response = new();
        try
        {
            var query = _context.TripOrigins.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Include(x => x.Station).Where(x => EF.Functions.ILike(x.Station!.Name, pattern) || EF.Functions.ILike(x.Station.Address!.Location, pattern)
                || EF.Functions.ILike(x.Station.Address!.State, pattern));
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

    // GET: api/Origins
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Origin>>> GetTripOrigins()
    {
        return await _context.TripOrigins.ToListAsync();
    }

    // GET: api/Origins/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Origin>> GetOrigin(Guid id)
    {
        var origin = await _context.TripOrigins.FindAsync(id);

        if (origin == null)
        {
            return NotFound();
        }

        return origin;
    }

    // PUT: api/Origins/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutOrigin(Guid id, Origin origin)
    {
        if (id != origin.Id)
        {
            return BadRequest();
        }

        _context.Entry(origin).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OriginExists(id))
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

    // POST: api/Origins
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Origin>> PostOrigin(Origin origin)
    {
        _context.TripOrigins.Add(origin);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetOrigin", new { id = origin.Id }, origin);
    }

    // DELETE: api/Origins/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrigin(Guid id)
    {
        var origin = await _context.TripOrigins.FindAsync(id);
        if (origin == null)
        {
            return NotFound();
        }

        _context.TripOrigins.Remove(origin);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool OriginExists(Guid id)
    {
        return _context.TripOrigins.Any(e => e.Id == id);
    }
}
