using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Logging;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditsController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<AuditLog>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<AuditLog> response = new();
        try
        {
            var query = _context.AuditLogs.AsQueryable();
            
            // if (!string.IsNullOrEmpty(request.SearchTerm))
            // {
            //     string pattern = $"%{request.SearchTerm}%";
            //     query = query.Include(x => x.Station).Where(x => EF.Functions.ILike(x.Station.Name, pattern) || EF.Functions.ILike(x.Station.Address!.Location, pattern)
            //     || EF.Functions.ILike(x.Station.Address!.State, pattern));
            // }

            response.Total = await query.CountAsync();
            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.Timestamp).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

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

    // GET: api/Audits
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs()
    {
        return await _context.AuditLogs.ToListAsync();
    }

    // GET: api/Audits/5
    [HttpGet("{id}")]
    public async Task<ActionResult<AuditLog>> GetAuditLog(Guid id)
    {
        var auditLog = await _context.AuditLogs.FindAsync(id);

        if (auditLog == null)
        {
            return NotFound();
        }

        return auditLog;
    }

    // PUT: api/Audits/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAuditLog(Guid id, AuditLog auditLog)
    {
        if (id != auditLog.Id)
        {
            return BadRequest();
        }

        _context.Entry(auditLog).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AuditLogExists(id))
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

    // POST: api/Audits
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<AuditLog>> PostAuditLog(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetAuditLog", new { id = auditLog.Id }, auditLog);
    }

    // DELETE: api/Audits/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAuditLog(Guid id)
    {
        var auditLog = await _context.AuditLogs.FindAsync(id);
        if (auditLog == null)
        {
            return NotFound();
        }

        _context.AuditLogs.Remove(auditLog);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool AuditLogExists(Guid id)
    {
        return _context.AuditLogs.Any(e => e.Id == id);
    }
}
