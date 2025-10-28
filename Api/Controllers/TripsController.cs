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

    [HttpGet("generate-dispatch")]
    public async Task<ActionResult<string>> GenerateDispatchId(Guid truckId, string date, CancellationToken cancellationToken)
    {
        var truck = await _context.Trucks.FindAsync(truckId);
        if (truck == null)
        {
            return NotFound("Truck not found");
        }
        var parsedDate = DateOnly.ParseExact(date, "yyyy-MM-dd");
        var baseDispatchId = (parsedDate.ToString("yyMMdd") + truck.LicensePlate?.Substring(2, 6)).Trim();
        
        var sameDayTrips = await _context.Trips
            .Where(t => t.TruckId == truckId && EF.Functions.ILike(t.DispatchId, $"{baseDispatchId}%"))
            .Select(t => t.DispatchId)
            .ToListAsync(cancellationToken);


        if (!(sameDayTrips.Count > 0 && !sameDayTrips.Contains($"-")))
            return Ok(baseDispatchId);


        int suffix = 1;
        while (sameDayTrips.Contains($"{baseDispatchId}-{suffix}"))
        {
            suffix++;
        }

        return Ok($"{baseDispatchId}-{suffix}");
    }

    [HttpGet("dispatch-exist")]
    public async Task<ActionResult<bool>> DispatchExistAsync(Guid truckId, string date, CancellationToken cancellationToken)
    {
        var truck = await _context.Trucks.FindAsync(truckId);
        if (truck == null)
        {
            return NotFound(false);
        }
        var parsedDate = DateOnly.ParseExact(date, "yyyy-MM-dd");
        var baseDispatchId = (parsedDate.ToString("yyMMdd") + truck.LicensePlate?.Substring(2, 6)).Trim();

        var start = parsedDate.ToDateTime(TimeOnly.MinValue); // 00:00
        var end = parsedDate.ToDateTime(TimeOnly.MaxValue);   // 23:59:59

        var exist = await _context.Trips.AnyAsync(t => t.TruckId == truckId && t.Date >= start && t.Date <= end);
        return Ok(exist);
    }

    // [HttpPost("report")]
    // public async Task<IActionResult> ExportReport([FromBody]ReportFilter request, CancellationToken cancellationToken = default)
    // {
    //     var query = _context.Trips
    //         .Include(x => x.Driver)
    //         .Include(x => x.Truck)
    //         .Include(x => x.LoadingDepot)
    //         .Include(x => x.Discharges)
    //         .ThenInclude(x => x.Station)
    //         .Include(x => x.ClosedBy)
    //         .Include(x => x.CompletedBy)
    //         .Where(x => x.Date.Month == request.StartDate.Month && x.Date.Year == request.StartDate.Year)
    //         .AsSplitQuery()
    //         .AsQueryable();

    //     if (request.Product is not null && request.Product != "")
    //     {
    //         query = query.Where(t => t.Truck.Product.ToString() == request.Product);
    //     }
        
    //     if (request.EndDate.HasValue)
    //     {
    //         var endDateTime = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
    //         query = query.Where(t => t.Date <= endDateTime);            
    //     }
        
    //     var csv = new StringBuilder();
    //     csv.AppendLine("Date,Dispatch Id,Truck Number,Product,Status,Loading Point,Loading Date,Waybill No,Dispatch Quantity,Driver Name,Destination,Elock Status,Arrived At ATV,ATV Arrival Date, Invoice Date, Arrived At Station,Station Arrival Date,Discharged,Discharge Location,Discharged Date,Discharged Quantity,Discharged Unit,Has Shortage,Shortage Amount,Return Date,Duration Days,Discharge Summary,Notes");

    //     var trips = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    //     var report = TripMapper.ToExportDto(trips);
    //     foreach (var s in report)
    //     {
    //         csv.AppendLine($"{EscapeCsv(s.Date.ToString("dd/MM/yyyy"))},{EscapeCsv(s.DispatchId)},{EscapeCsv(s.TruckPlate)},{EscapeCsv(s.Product)},{EscapeCsv(s.Status)},{EscapeCsv(s.LoadingPoint)},{EscapeCsv(s.LoadingDate)},{EscapeCsv(s.WaybillNo)},{EscapeCsv(s.DispatchQuantity.ToString())},{EscapeCsv(s.DriverName)},{EscapeCsv(s.Dest)},{EscapeCsv(s.ElockStatus)},{EscapeCsv(s.ArrivedAtATV)},{EscapeCsv(s.AtvArrivalDate)}, {EscapeCsv(s.InvoiceDate)},{EscapeCsv(s.ArrivedAtStation)},{EscapeCsv(s.StationArrivalDate)},{EscapeCsv(s.Discharged)},{EscapeCsv(s.DischargeLocation)},{EscapeCsv(s.DischargedDate)},{s.DischargedQuantity},{EscapeCsv(s.DischargedUnit)},{EscapeCsv(s.HasShortage)},{s.ShortageAmount},{EscapeCsv(s.ReturnDate)},{s.DurationDays},{EscapeCsv(s.DischargeSummary)},{EscapeCsv(s.Notes)}");
    //     }       
    //     var bytes = Encoding.UTF8.GetBytes(csv.ToString());
    //     var stream = new MemoryStream(bytes);

    //     return new FileStreamResult(stream, "text/csv")
    //     {
    //         FileDownloadName = $"Loading from {request.StartDate:MMMM-yyyy} {(request.EndDate.HasValue ? $"to {request.EndDate:MMMM-yyyy}" : "")}.csv"
    //     };        
    // }
    
    [HttpPost("report")]
    public async Task<IActionResult> ExportReport([FromBody] ReportFilter request, CancellationToken cancellationToken = default)
    {
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.ReceivingDepot)
            .Include(x => x.Discharges).ThenInclude(x => x.Station)
            .Include(x => x.ClosedBy)
            .Include(x => x.CompletedBy)
            .Where(x => x.Date.Month == request.StartDate.Month && x.Date.Year == request.StartDate.Year)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Product))
            query = query.Where(t => t.Truck.Product.ToString() == request.Product);

        if (request.EndDate.HasValue)
        {
            var endDateTime = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(t => t.Date <= endDateTime);
        }

        var trips = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var report = TripMapper.ToExportDto(trips);

        var csv = new StringBuilder();
        
        csv.AppendLine(
            "S/N,Dispatch Date,Loading Depot Arrival Date,Loading Date,Truck Number,Product,Trip Status,Loading Point,Waybill Number,Dispatch Quantity,Driver Name,Destination,E-lock Status,Dispatch Type,Arrived Depot,Depot Arrival Date,Depot Name,Invoiced,Invoice Date,Arrived Station,Station Arrival Date,Discharging/Discharged?,Discharged Date,Discharge Location,Discharged Quantity,Unit (SCM/KG/MT/LTR),Return Date,Shortage/Overage?,Shortage/Overage Amount,Remarks,Duration Days,Discharge Summary"
        );

        int sn = 1;
        foreach (var s in report)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                sn++.ToString(),
                EscapeCsv(s.Date.ToString("dd/MM/yyyy")),
                EscapeCsv(s.LoadingDepotDate),
                EscapeCsv(s.LoadingDate),
                EscapeCsv(s.TruckPlate),
                // EscapeCsv(s.DispatchId),
                EscapeCsv(s.Product),
                EscapeCsv(s.Status),
                EscapeCsv(s.LoadingPoint),
                EscapeCsv(s.WaybillNo),
                EscapeCsv(s.DispatchQuantity.ToString("N2")),
                EscapeCsv(s.DriverName),
                EscapeCsv(s.Dest),
                EscapeCsv(s.ElockStatus),
                EscapeCsv(s.DispatchType),
                EscapeCsv(s.ArrivedDepot),
                EscapeCsv(s.DepotArrival),
                EscapeCsv(s.DepotName),
                EscapeCsv(s.Invoiced),
                EscapeCsv(s.InvoiceDate),
                EscapeCsv(s.ArrivedStation),
                EscapeCsv(s.StationArrivalDate),
                EscapeCsv(s.Discharged),
                EscapeCsv(s.DischargedDate),
                EscapeCsv(s.DischargeLocation),
                EscapeCsv(s.DischargedQuantity.ToString("N2")),
                EscapeCsv(s.DischargedUnit),
                EscapeCsv(s.ReturnDate),
                EscapeCsv(s.HasShortage),
                EscapeCsv(s.ShortageAmount?.ToString("N2")),
                EscapeCsv(s.Notes),
                EscapeCsv(s.DurationDays.ToString()),
                EscapeCsv(s.DischargeSummary)
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var stream = new MemoryStream(bytes);

        var fileName = $"Trip_Report_{request.StartDate:MMMM-yyyy}" +
                    (request.EndDate.HasValue ? $"_to_{request.EndDate:MMMM-yyyy}" : "") +
                    ".csv";

        return new FileStreamResult(stream, "text/csv")
        {
            FileDownloadName = fileName
        };
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Replace("\"", "\"\"");
        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            value = $"\"{value}\"";

        return value;
    }


    [HttpPost("trips-byrange")]
    public async Task<ActionResult<IEnumerable<Trip>>> TripsByRange(ReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Trips.Include(x => x.Discharges).Include(x => x.Truck).Where(x =>  x.Truck.Product != Shared.Enums.Product.CNG && x.Date >= filter.StartDate.ToDateTime(TimeOnly.MinValue) && x.Date <= filter.EndDate.Value.ToDateTime(TimeOnly.MinValue)).ToArrayAsync(cancellationToken);
        }
        catch (System.Exception)
        {
            
            throw;
        }
    }

    // private string EscapeCsv(string? value)
    // {
    //     if (string.IsNullOrEmpty(value)) return "";
    //     if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
    //         return $"\"{value.Replace("\"", "\"\"")}\"";
    //     return value;
    // }

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

            if (!string.IsNullOrEmpty(request.Product))
            {
                query = query.Where(t => t.Truck.Product.ToString() == request.Product);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    string pattern = $"%{request.SearchTerm}%";
                    query = query.Include(x => x.Truck)
                                .Include(x => x.Driver)
                                .AsSplitQuery()
                                .Where(x => EF.Functions.ILike(x.LoadingInfo.WaybillNo!, pattern)
                                || EF.Functions.ILike(x.LoadingDepot.Name, pattern)
                                || EF.Functions.ILike(x.DispatchId, pattern)
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

            return response;        
        }
        catch (Exception)
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
                                .Include(x => x.Discharges)
                                .ThenInclude(x => x.InvoicedStation)
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
        var dispatchResult = await GenerateDispatchId(trip.TruckId, trip.Date.ToString("yyyy-MM-dd"), CancellationToken.None);
        if (dispatchResult.Result is OkObjectResult okResult && okResult.Value is string dispatchId)
        {
            Console.WriteLine($"Generated DispatchId: {dispatchId}");
            trip.DispatchId = dispatchId.Trim();
        }
        else
        {
            return BadRequest("Failed to generate DispatchId.");
        }
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
