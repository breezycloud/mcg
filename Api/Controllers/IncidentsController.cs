using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Incidents;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IncidentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public IncidentsController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Incident>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Incident> response = new();
        try
        {
            var query = _context.Incidents.Include(x => x.Truck)
                                          .Include(x => x.Driver)
                                          .Include(x => x.Trip)
                                          .Include(x => x.IncidentType)
                                          .Include(x => x.History)
                                          .Include(x => x.CreatedBy)
                                          .Include(x => x.TreatedBy)
                                          .Include(x => x.ClosedBy)
                                          .AsNoTracking()
                                          .AsSplitQuery()
                                          .AsQueryable();

            if (request.Id.HasValue)
            {
                query = query.Where(x => x.TripId == request.Id.Value || x.DriverId == request.Id.Value || x.TruckId == request.Id.Value || x.CreatedById == request.Id.Value);
            }
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Truck!.TruckNo, pattern));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                string pattern = $"%{request.Status}%";
                query = query.Where(x => EF.Functions.ILike(x.Status.ToString()!, pattern));
            }
            
            response.Total = await query.CountAsync(cancellationToken);

            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.TreatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

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


    // GET: api/Incidents
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Incident>>> GetIncident()
    {
        return await _context.Incidents.ToListAsync();
    }

    // GET: api/Incidents/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Incident>> GetIncident(Guid id)
    {
        var incident = await _context.Incidents.AsNoTracking()
                                                .Include(x => x.Trip)
                                                .Include(x => x.Truck)
                                                .Include(x => x.Driver)
                                                .Include(x => x.IncidentType)
                                                .Include(x => x.CreatedBy)
                                                .Include(x => x.TreatedBy)
                                                .Include(x => x.ClosedBy)
                                                .Include(x => x.History).ThenInclude(x => x.ChangedBy)
                                                .AsSplitQuery()
                                                .FirstOrDefaultAsync(x => x.Id == id);

        if (incident == null)
        {
            return NotFound();
        }

        return incident;
    }

    // PUT: api/Incidents/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutIncident(Guid id, Incident incident)
    {
        if (id != incident.Id)
        {
            return BadRequest();
        }

        _context.Entry(incident).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!IncidentExists(id))
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

    // POST: api/Incidents
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Incident>> PostIncident(Incident incident)
    {
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetIncident", new { id = incident.Id }, incident);
    }

    // POST: api/Incidents/History
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost("History")]
    public async Task<ActionResult<IncidentHistory>> PostIncidentHistory(IncidentHistory history, CancellationToken cancellationToken)
    {
        var exist = await _context.IncidentHistories.FindAsync(history.Id);
        if (exist is not null)
        {
            exist.ChangedAt = history.ChangedAt;
            exist.Status = history.Status;
            exist.Notes = history.Notes;
        }
        else
        {
            _context.IncidentHistories.Add(history);
        }        
        await _context.SaveChangesAsync(cancellationToken);

        return Created();
    }

    // DELETE: api/Incidents/5
    [HttpDelete("History/Delete/{id}")]
    public async Task<IActionResult> DeleteIncidentHistory(Guid id)
    {
        var history = await _context.IncidentHistories.FindAsync(id);
        if (history == null)
        {
            return NotFound();
        }

        _context.IncidentHistories.Remove(history);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Incidents/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncident(Guid id)
    {
        var incident = await _context.Incidents.FindAsync(id);
        if (incident == null)
        {
            return NotFound();
        }

        _context.Incidents.Remove(incident);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool IncidentExists(Guid id)
    {
        return _context.Incidents.Any(e => e.Id == id);
    }
}
