using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.TripCheckpoints;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripCheckpointsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TripCheckpointsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<TripCheckpoint>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<TripCheckpoint> response = new();
        try
        {
            var query = _context.TripCheckpoints.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Include(x => x.Checkpoint).Where(x => EF.Functions.ILike(x.Checkpoint!.Name!, pattern) );
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

    // GET: api/TripCheckpoints
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripCheckpoint>>> GetTripCheckpoints()
    {
        return await _context.TripCheckpoints.ToListAsync();
    }

    // GET: api/TripCheckpoints/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TripCheckpoint>> GetCheckpoint(Guid id)
    {
        var TripCheckpoint = await _context.TripCheckpoints.FindAsync(id);

        if (TripCheckpoint == null)
        {
            return NotFound();
        }

        return TripCheckpoint;
    }

    // PUT: api/TripCheckpoints/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCheckpoint(Guid id, TripCheckpoint TripCheckpoint)
    {
        if (id != TripCheckpoint.Id)
        {
            return BadRequest();
        }

        _context.Entry(TripCheckpoint).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CheckpointExists(id))
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

    // POST: api/TripCheckpoints
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TripCheckpoint>> PostCheckpoint(TripCheckpoint TripCheckpoint)
    {
        _context.TripCheckpoints.Add(TripCheckpoint);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetCheckpoint", new { id = TripCheckpoint.Id }, TripCheckpoint);
    }

    // DELETE: api/TripCheckpoints/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCheckpoint(Guid id)
    {
        var TripCheckpoint = await _context.TripCheckpoints.FindAsync(id);
        if (TripCheckpoint == null)
        {
            return NotFound();
        }

        _context.TripCheckpoints.Remove(TripCheckpoint);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CheckpointExists(Guid id)
    {
        return _context.TripCheckpoints.Any(e => e.Id == id);
    }
}
