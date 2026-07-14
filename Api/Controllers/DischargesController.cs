using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Api.Services.Discharges;
using Shared.Models.MessageBroker;
using Shared.Models.Trips;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DischargesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ShortageNotificationService _shortageNotificationService;

    public DischargesController(AppDbContext context, ShortageNotificationService shortageNotificationService)
    {
        _context = context;
        _shortageNotificationService = shortageNotificationService;
    }


     // POST: api/Paged
    // [HttpPost("paged")]
    // public async Task<ActionResult<GridDataResponse<Discharge>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    // {
    //     GridDataResponse<Discharge> response = new();
    //     try
    //     {
    //         var query = _context.Discharges.Include(x => x.Driver).Include(x => x.Truck).Include(x => x.Origin).ThenInclude(x => x!.Station).Include(x=>x.Destination).ThenInclude(x=> x.Station).AsSplitQuery().AsQueryable();

    //         if (request.Id.HasValue)
    //         {
    //             query = query.Where(x => x.Origin!.Id == request.Id);
    //         }
            
    //         if (!string.IsNullOrEmpty(request.SearchTerm))
    //             {
    //                 string pattern = $"%{request.SearchTerm}%";
    //                 query = query.Include(x => x.Truck).Include(x => x.Destination).ThenInclude(x => x!.Station)
    //                             .AsSplitQuery()
    //                             .Where(x => EF.Functions.ILike(x.WaybillNo!, pattern)
    //                             || EF.Functions.ILike(x.Origin!.Station!.Address!.State, pattern)
    //                             || EF.Functions.ILike(x.Origin!.Station!.Address!.Location, pattern)
    //                             || EF.Functions.ILike(x.Origin!.Station!.Address!.ContactAddress!, pattern)
    //                             || EF.Functions.ILike(x.Destination!.Station!.Address!.State, pattern)
    //                             || EF.Functions.ILike(x.Destination!.Station!.Address!.Location, pattern)
    //                             || EF.Functions.ILike(x.Destination!.Station!.Address!.ContactAddress!, pattern));
    //             }

    //         response.Total = await query.CountAsync();
    //         response.Data = [];
    //         var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable();

    //         await foreach (var item in pagedQuery)
    //         {
    //             response.Data.Add(item);
    //         }


    //         return response;

            
    //     }
    //     catch (System.Exception)
    //     {

    //         throw;
    //     }
    // }

    // GET: api/Discharges
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Discharge>>> GetDischarges()
    {
        return await _context.Discharges.ToListAsync();
    }

    // GET: api/Discharges/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Discharge>> GetDischarge(Guid id)
    {
        var trip = await _context.Discharges.FindAsync(id);

        if (trip == null)
        {
            return NotFound();
        }

        return trip;
    }

    // PUT: api/Discharges/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutDischarge(Guid id, Discharge trip)
    {
        if (id != trip.Id)
        {
            return BadRequest();
        }

        _context.Entry(trip).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!DischargeExists(id))
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

    // POST: api/Discharges
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Discharge>> PostDischarge(Discharge trip)
    {
        _context.Discharges.Add(trip);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetDischarge", new { id = trip.Id }, trip);
    }

    // GET: api/Discharges/5/shortage-preview — renders exactly what a CCU notification for this
    // discharge would look like, without sending anything. Backs the "Preview & Send to CCU"
    // button on ViewTrip.razor.
    [HttpGet("{id}/shortage-preview")]
    public async Task<ActionResult<ShortagePreviewDto>> GetShortagePreview(Guid id, CancellationToken cancellationToken)
    {
        return await _shortageNotificationService.GetPreviewAsync(id);
    }

    // POST: api/Discharges/5/send-shortage-notification — the only place a CCU shortage email
    // actually gets queued; only ever called from the preview modal's explicit "Send" click.
    [HttpPost("{id}/send-shortage-notification")]
    public async Task<IActionResult> SendShortageNotification(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await _shortageNotificationService.SendAsync(id);
        if (!success)
        {
            return BadRequest(new { error });
        }
        return NoContent();
    }

    // DELETE: api/Discharges/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDischarge(Guid id)
    {
        var trip = await _context.Discharges.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        _context.Discharges.Remove(trip);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool DischargeExists(Guid id)
    {
        return _context.Discharges.Any(e => e.Id == id);
    }
}
