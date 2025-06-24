using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.RefuelInfos;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RefuelInfosController : ControllerBase
{
    private readonly AppDbContext _context;

    public RefuelInfosController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<RefuelInfo>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<RefuelInfo> response = new();
        try
        {
            var query = _context.RefuelInfos.Include(x => x.Truck).Include(x => x.Station).AsSplitQuery().AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Station!.Name, pattern) );
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

    // GET: api/RefuelInfos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RefuelInfo>>> GetRefuelInfos()
    {
        return await _context.RefuelInfos.ToListAsync();
    }

    // // GET: api/RefuelInfos
    // [HttpGet("type")]
    // public async Task<ActionResult<IEnumerable<RefuelInfo>>> GetRefuelInfos(string type, CancellationToken cancellationToken)
    // {
    //     IQueryable<RefuelInfo> query;
    //     List<RefuelInfo> stations = [];
    //     try
    //     {
    //         query = _context.RefuelInfos.Where(x => EF.Functions.ILike(x.Type.ToString(), $"%{type}%")).AsQueryable();

    //         await foreach (var RefuelInfo in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
    //         {
    //             stations.Add(RefuelInfo);
    //         }
    //         return stations;
    //     }
    //     catch (System.Exception)
    //     {

    //         throw;
    //     }
    // }

    // GET: api/RefuelInfos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<RefuelInfo>> GetRefuelInfo(Guid id)
    {
        var RefuelInfo = await _context.RefuelInfos.FindAsync(id);

        if (RefuelInfo == null)
        {
            return NotFound();
        }

        return RefuelInfo;
    }

    // PUT: api/RefuelInfos/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRefuelInfo(Guid id, RefuelInfo RefuelInfo)
    {
        if (id != RefuelInfo.Id)
        {
            return BadRequest();
        }

        _context.Entry(RefuelInfo).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RefuelInfoExists(id))
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

    // POST: api/RefuelInfos
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<RefuelInfo>> PostRefuelInfo(RefuelInfo RefuelInfo)
    {
        _context.RefuelInfos.Add(RefuelInfo);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetRefuelInfo", new { id = RefuelInfo.Id }, RefuelInfo);
    }

    // DELETE: api/RefuelInfos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRefuelInfo(Guid id)
    {
        var RefuelInfo = await _context.RefuelInfos.FindAsync(id);
        if (RefuelInfo == null)
        {
            return NotFound();
        }

        _context.RefuelInfos.Remove(RefuelInfo);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool RefuelInfoExists(Guid id)
    {
        return _context.RefuelInfos.Any(e => e.Id == id);
    }
}
