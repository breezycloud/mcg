using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Shops;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MaintenanceSitesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MaintenanceSitesController(AppDbContext context)
    {
        _context = context;
    }


    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<MaintenanceSite>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<MaintenanceSite> response = new();
        try
        {
            var query = _context.MaintenanceSites.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Name!, pattern) || EF.Functions.ILike(x.State!, pattern)
                || EF.Functions.ILike(x.Location!, pattern));
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

    // GET: api/MaintenanceSites
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MaintenanceSite>>> GetMaintenanceSites()
    {
        return await _context.MaintenanceSites.ToListAsync();
    }

    // GET: api/MaintenanceSites/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MaintenanceSite>> GetMaintenanceSite(Guid id)
    {
        var maintenanceSite = await _context.MaintenanceSites.FindAsync(id);

        if (maintenanceSite == null)
        {
            return NotFound();
        }

        return maintenanceSite;
    }

    // PUT: api/MaintenanceSites/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutMaintenanceSite(Guid id, MaintenanceSite maintenanceSite)
    {
        if (id != maintenanceSite.Id)
        {
            return BadRequest();
        }

        _context.Entry(maintenanceSite).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MaintenanceSiteExists(id))
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

    // POST: api/MaintenanceSites
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<MaintenanceSite>> PostMaintenanceSite(MaintenanceSite maintenanceSite)
    {
        _context.MaintenanceSites.Add(maintenanceSite);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetMaintenanceSite", new { id = maintenanceSite.Id }, maintenanceSite);
    }

    // DELETE: api/MaintenanceSites/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMaintenanceSite(Guid id)
    {
        var maintenanceSite = await _context.MaintenanceSites.FindAsync(id);
        if (maintenanceSite == null)
        {
            return NotFound();
        }

        _context.MaintenanceSites.Remove(maintenanceSite);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool MaintenanceSiteExists(Guid id)
    {
        return _context.MaintenanceSites.Any(e => e.Id == id);
    }
}
