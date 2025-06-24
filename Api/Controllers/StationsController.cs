using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Stations;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StationsController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Station>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Station> response = new();
        try
        {
            var query = _context.Stations.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Name, pattern)
                || EF.Functions.ILike(x.Address!.State, pattern)
                || EF.Functions.ILike(x.Address!.Location, pattern)
                || EF.Functions.ILike(x.Address!.ContactAddress!, pattern)
                || EF.Functions.ILike(x.Type.ToString(), pattern));
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

    // GET: api/Stations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Station>>> GetStations()
    {
        return await _context.Stations.ToListAsync();
    }

    // GET: api/Stations
    [HttpGet("type")]
    public async Task<ActionResult<IEnumerable<Station>>> GetStations(string type, CancellationToken cancellationToken)
    {
        IQueryable<Station> query;
        List<Station> stations = [];
        try
        {
            query = _context.Stations.Where(x => EF.Functions.ILike(x.Type.ToString(), $"%{type}%")).AsQueryable();

            await foreach (var station in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                stations.Add(station);
            }
            return stations;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Stations/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Station>> GetStation(Guid id)
    {
        var station = await _context.Stations.FindAsync(id);

        if (station == null)
        {
            return NotFound();
        }

        return station;
    }

    // PUT: api/Stations/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutStation(Guid id, Station station)
    {
        if (id != station.Id)
        {
            return BadRequest();
        }

        _context.Entry(station).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StationExists(id))
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

    // POST: api/Stations
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Station>> PostStation(Station station)
    {
        _context.Stations.Add(station);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetStation", new { id = station.Id }, station);
    }

    // DELETE: api/Stations/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStation(Guid id)
    {
        var station = await _context.Stations.FindAsync(id);
        if (station == null)
        {
            return NotFound();
        }

        _context.Stations.Remove(station);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool StationExists(Guid id)
    {
        return _context.Stations.Any(e => e.Id == id);
    }
}
