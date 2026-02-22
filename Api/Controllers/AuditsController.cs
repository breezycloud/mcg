using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Logging;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuditsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditsController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/Audits/paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<AuditLog>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<AuditLog> response = new();
        try
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                string term = request.SearchTerm.ToLower();
                query = query.Where(x =>
                    (x.UserName != null && x.UserName.ToLower().Contains(term)) ||
                    (x.EntityType != null && x.EntityType.ToLower().Contains(term)) ||
                    (x.EntityId != null && x.EntityId.ToLower().Contains(term)) ||
                    (x.AffectedFields != null && x.AffectedFields.ToLower().Contains(term)) ||
                    (x.IpAddress != null && x.IpAddress.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(x => x.EntityType == request.EntityType);

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(x => x.Action == request.Action);

            if (request.FromDate.HasValue)
                query = query.Where(x => x.Timestamp >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(x => x.Timestamp <= request.ToDate.Value.AddDays(1));

            response.Total = await query.CountAsync(cancellationToken);
            response.Data = await query
                .OrderByDescending(x => x.Timestamp)
                .Skip(request.Paging)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

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
