using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Trips;
using Shared.Helpers;
using Shared.Dtos;
using System.Text;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TripsController(AppDbContext context)
    {
        _context = context;

    }

    [HttpPost("report")]
    public async Task<IActionResult> ExportReport([FromBody]ReportFilter request, CancellationToken cancellationToken = default)
    {
        // Build date range for selected months in the given year        
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.Discharges)
            .ThenInclude(x => x.Station)
            .Include(x => x.ClosedBy)
            .Include(x => x.CompletedBy)
            .Where(x => x.Date.Month == request.StartDate.Month && x.Date.Year == request.StartDate.Year)
            .AsSplitQuery()
            .AsQueryable();

        if (request.EndDate.HasValue)
        {
            query = query.Where(x => x.Date <= request.EndDate.Value);
        }
        
        var csv = new StringBuilder();
        csv.AppendLine("Date,Dispatch Id,Truck Number,Product,Status,Loading Point,Waybill No,Dispatch Quantity,Driver Name,Destination,Elock Status,Arrived At ATV,ATV Arrival Date, Invoice Date, Arrived At Station,Station Arrival Date,Discharged,Discharge Location,Discharged Date,Discharged Quantity,Discharged Unit,Has Shortage,Shortage Amount,Return Date,Duration Days,Discharge Summary,Notes");

        var trips = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

        var report = TripMapper.ToExportDto(trips);
        foreach (var s in report)
        {
            csv.AppendLine($"{EscapeCsv(s.Date.ToString("dd/MM/yyyy"))},{EscapeCsv(s.DispatchId)},{EscapeCsv(s.TruckPlate)},{EscapeCsv(s.Product)},{EscapeCsv(s.Status)},{EscapeCsv(s.LoadingPoint)},{EscapeCsv(s.WaybillNo)},{EscapeCsv(s.DispatchQuantity.ToString())},{EscapeCsv(s.DriverName)},{EscapeCsv(s.Dest)},{EscapeCsv(s.ElockStatus)},{EscapeCsv(s.ArrivedAtATV)},{EscapeCsv(s.AtvArrivalDate)}, {EscapeCsv(s.InvoiceDate)},{EscapeCsv(s.ArrivedAtStation)},{EscapeCsv(s.StationArrivalDate)},{EscapeCsv(s.Discharged)},{EscapeCsv(s.DischargeLocation)},{EscapeCsv(s.DischargedDate)},{s.DischargedQuantity},{EscapeCsv(s.DischargedUnit)},{EscapeCsv(s.HasShortage)},{s.ShortageAmount},{EscapeCsv(s.ReturnDate)},{s.DurationDays},{EscapeCsv(s.DischargeSummary)},{EscapeCsv(s.Notes)}");
        }       
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var stream = new MemoryStream(bytes);

        return new FileStreamResult(stream, "text/csv")
        {
            FileDownloadName = $"Loading from {request.StartDate:MMMM-yyyy} {(request.EndDate.HasValue ? $"to {request.EndDate:MMMM-yyyy}" : "")}.csv"
        };        
    }

    [HttpPost("trips-byrange")]
    public async Task<ActionResult<IEnumerable<Trip>>> TripsByRange(ReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Trips.Include(x => x.Discharges).Include(x => x.Truck).Where(x =>  x.Truck.Product != Shared.Enums.Product.CNG && x.Date >= filter.StartDate && x.Date <= filter.EndDate.Value).ToArrayAsync(cancellationToken);
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Trip>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Trip> response = new();
        try
        {
            var query = _context.Trips.AsNoTracking()
                                      .Include(x => x.Driver)
                                      .Include(x => x.Truck)
                                      .Include(x => x.LoadingDepot)
                                      .Include(x => x.Discharges)
                                      .ThenInclude(x => x.Station)
                                      .Include(x => x.ClosedBy)
                                      .Include(x => x.CompletedBy)                                                                            
                                      .AsSplitQuery()
                                      .AsQueryable();
            if (request.Date is not null)
            {
                query = query.Where(x => x.Date.Month == request.Date.Value.Month && x.Date.Year == request.Date.Value.Year);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    string pattern = $"%{request.SearchTerm}%";
                    query = query.Include(x => x.Truck)
                                .Include(x => x.Driver)
                                .AsSplitQuery()
                                .Where(x => EF.Functions.ILike(x.LoadingInfo.WaybillNo!, pattern)
                                || EF.Functions.ILike(x.LoadingDepot.Name, pattern)
                                || EF.Functions.ILike(x.LoadingInfo.Destination, pattern)
                                || EF.Functions.ILike(x.Truck.LicensePlate, pattern)
                                || EF.Functions.ILike(x.Truck.TruckNo, pattern)
                                || EF.Functions.ILike(x.Driver.LastName, pattern)
                                || EF.Functions.ILike(x.Driver.FirstName, pattern));
                }

            if (!string.IsNullOrEmpty(request.Status))
            {
                string pattern = $"%{request.Status}%";
                query = query.Where(x => EF.Functions.ILike(x.Status.ToString()!, pattern));
            }

            response.Total = await query.CountAsync();
            response.Data = [];
            response.Data = await query.OrderByDescending(x => x.Date)
                                  .ThenByDescending(x => x.LoadingInfo.LoadingDate)
                                  .Skip(request.Paging)
                                  .Take(request.PageSize)
                                  .ToListAsync();

            // await foreach (var item in pagedQuery)
            // {
            //     response.Data.Add(item);
            // }


            return response;

            
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Trips
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Trip>>> GetTrips()
    {
        return await _context.Trips.ToListAsync();
    }

    // GET: api/Trips/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Trip>> GetTrip(Guid id)
    {
        var trip = await _context.Trips.AsNoTracking()
                                .Include(x => x.Driver)
                                .Include(x => x.Truck)
                                .Include(x => x.LoadingDepot)
                                .Include(x => x.ReceivingDepot)
                                .Include(x => x.Discharges)
                                .ThenInclude(x => x.Station)
                                .Include(x => x.Incidents)
                                .ThenInclude(x => x.IncidentType)
                                .Include(x => x.ClosedBy)
                                .Include(x => x.CompletedBy)
                                .AsSplitQuery()
                                .FirstOrDefaultAsync(x => x.Id == id);

        if (trip is null)
        {
            return NotFound();
        }
        return trip;
    }

    // GET: api/Trips/5
    [HttpGet("Active/{id}")]
    public async Task<ActionResult<Trip>> GetActiveTrip(Guid id)
    {
        var trip = await _context.Trips.AsNoTracking().FirstOrDefaultAsync(x => x.TruckId == id);

        if (trip is null)
        {
            return NotFound();
        }
        return trip;
    }

    [HttpGet("truck-trips/{id}")]
    public async Task<ActionResult<List<Trip>>> GetActiveTrip(Guid id, int year, CancellationToken cancellationToken = default)
    {
        var trips = await _context.Trips.Where(x => x.TruckId == id && x.Date.Year == year)
                                       .OrderByDescending(x => x.Date)
                                       .ToListAsync(cancellationToken);

        return trips;
    }

    // PUT: api/Trips/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTrip(Guid id, Trip trip)
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
            if (!TripExists(id))
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

    // POST: api/Trips
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Trip>> PostTrip(Trip trip)
    {
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTrip", new { id = trip.Id }, trip);
    }

    // DELETE: api/Trips/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null)
        {
            return NotFound();
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TripExists(Guid id)
    {
        return _context.Trips.Any(e => e.Id == id);
    }
}
