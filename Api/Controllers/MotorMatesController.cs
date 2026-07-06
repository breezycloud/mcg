using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Dtos;
using Shared.Models.Drivers;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MotorMatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MotorMatesController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/MotorMates/paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<MotorMate>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<MotorMate> response = new();
        var query = _context.MotorMates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            string pattern = $"%{request.SearchTerm}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.PhoneNo!, pattern));
        }

        response.Total = await query.CountAsync(cancellationToken);
        response.Data = [];
        var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt)
            .Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable().WithCancellation(cancellationToken);

        await foreach (var item in pagedQuery)
        {
            response.Data.Add(item);
        }

        return response;
    }

    // GET: api/MotorMates
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MotorMate>>> GetMotorMates()
    {
        return await _context.MotorMates.AsNoTracking().ToListAsync();
    }

    // GET: api/MotorMates/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MotorMate>> GetMotorMate(Guid id)
    {
        var motorMate = await _context.MotorMates.Include(x => x.Drivers).FirstOrDefaultAsync(x => x.Id == id);
        if (motorMate == null)
        {
            return NotFound();
        }
        return motorMate;
    }

    // GET: api/MotorMates/validate?phone=
    [HttpGet("validate")]
    public async Task<ActionResult<PhoneValidationResult>> ValidatePhone(string phone, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNumberHelper.NormalizeForComparison(phone);
        if (normalized.Length == 0) return new PhoneValidationResult();

        var match = await _context.MotorMates.AsNoTracking()
            .Where(x => x.Id != excludeId)
            .Where(x => x.PhoneNo != null && x.PhoneNo.EndsWith(normalized))
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new PhoneValidationResult { MatchedId = match };
    }

    // PUT: api/MotorMates/5
    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMotorMate(Guid id, MotorMate motorMate)
    {
        if (id != motorMate.Id)
        {
            return BadRequest();
        }

        motorMate.UpdatedAt = DateTimeOffset.UtcNow;
        _context.Entry(motorMate).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MotorMateExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // POST: api/MotorMates
    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor")]
    [HttpPost]
    public async Task<ActionResult<MotorMate>> PostMotorMate(MotorMate motorMate)
    {
        _context.MotorMates.Add(motorMate);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetMotorMate", new { id = motorMate.Id }, motorMate);
    }

    // DELETE: api/MotorMates/5
    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMotorMate(Guid id)
    {
        var motorMate = await _context.MotorMates.FindAsync(id);
        if (motorMate == null)
        {
            return NotFound();
        }

        _context.MotorMates.Remove(motorMate);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool MotorMateExists(Guid id)
    {
        return _context.MotorMates.Any(e => e.Id == id);
    }
}
