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
public class IncidentTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public IncidentTypesController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<IncidentType>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<IncidentType> response = new();
        try
        {
            var query = _context.IncidentTypes.AsQueryable();

            if (request.Id.HasValue)
            {
                query = query.Where(x => x.Id == request.Id.Value);
            }
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Type, pattern));
            }

            

            response.Total = await query.CountAsync(cancellationToken);



            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

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


    // GET: api/IncidentTypes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidentType>>> GetIncident()
    {
        return await _context.IncidentTypes.ToListAsync();
    }

    // GET: api/IncidentTypes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<IncidentType>> GetIncident(Guid id)
    {
        var incident = await _context.IncidentTypes.AsNoTracking()                                                    
                                                    .Include(x => x.Incidents)
                                                    .AsSplitQuery()
                                                    .FirstOrDefaultAsync(x => x.Id == id);

        if (incident == null)
        {
            return NotFound();
        }

        return incident;
    }

    // PUT: api/IncidentTypes/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutIncident(Guid id, IncidentType incident)
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

    // POST: api/IncidentTypes
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<IncidentType>> PostIncident(IncidentType incident)
    {
        _context.IncidentTypes.Add(incident);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetIncident", new { id = incident.Id }, incident);
    }

    // POST: api/IncidentTypes/History
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

    // DELETE: api/IncidentTypes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIncident(Guid id)
    {
        var incident = await _context.IncidentTypes.FindAsync(id);
        if (incident == null)
        {
            return NotFound();
        }

        _context.IncidentTypes.Remove(incident);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool IncidentExists(Guid id)
    {
        return _context.IncidentTypes.Any(e => e.Id == id);
    }
}
