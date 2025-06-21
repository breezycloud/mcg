using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Drivers;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DriversController : ControllerBase
{
    private readonly AppDbContext _context;

    public DriversController(AppDbContext context)
    {
        _context = context;
    }


    
    // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Driver>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Driver> response = new();
        try
        {
            var query = _context.Drivers.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.FirstName!, pattern) || EF.Functions.ILike(x.LastName!, pattern)
                || EF.Functions.ILike(x.PhoneNo!, pattern));
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

    // GET: api/Drivers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers()
    {
        return await _context.Drivers.ToListAsync();
    }

    // GET: api/Drivers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Driver>> GetDriver(Guid id)
    {
        var driver = await _context.Drivers.FindAsync(id);

        if (driver == null)
        {
            return NotFound();
        }

        return driver;
    }

    // PUT: api/Drivers/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDriver(Guid id, Driver driver)
    {
        if (id != driver.Id)
        {
            return BadRequest();
        }

        _context.Entry(driver).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DriverExists(id))
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

    // POST: api/Drivers
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Driver>> PostDriver(Driver driver)
    {
        _context.Drivers.Add(driver);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetDriver", new { id = driver.Id }, driver);
    }

    // DELETE: api/Drivers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDriver(Guid id)
    {
        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null)
        {
            return NotFound();
        }

        _context.Drivers.Remove(driver);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DriverExists(Guid id)
    {
        return _context.Drivers.Any(e => e.Id == id);
    }
}
