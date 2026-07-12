using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Api.Context;
using Api.Filters;
using Api.Util;
using Api.Services.Discharges;
using Shared.Models.Trips;
using Shared.Helpers;
using Shared.Dtos;
using Shared.Enums;
using Shared.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TripsController : ControllerBase
{
    // Alphanumeric + dash only, 1-30 chars — same shape as DispatchCheckController's, used by
    // GetDispatchDetail (the get-dispatch action, the other endpoint external callers hit).
    private static readonly Regex DispatchIdRegex =
        new(@"^[a-zA-Z0-9\-]{1,30}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private readonly AppDbContext _context;
    private readonly ShortageNotificationService _shortageNotificationService;
    private readonly ILogger<TripsController> _logger;
    private readonly string _uploadPath;
    private readonly string _uploadRoot;

    public TripsController(AppDbContext context, ShortageNotificationService shortageNotificationService, ILogger<TripsController> logger, IConfiguration config, IHostEnvironment env)
    {
        _context = context;
        _shortageNotificationService = shortageNotificationService;
        _logger = logger;

        var rawPath = config["FileStorage:UploadPath"]!;
        _uploadPath = Path.IsPathRooted(rawPath)
            ? rawPath
            : Path.Combine(env.ContentRootPath, rawPath);

        // Canonical, trailing-separator form of the upload root, used to verify any resolved
        // file path is actually contained within it before opening (path-traversal guard).
        _uploadRoot = Path.GetFullPath(_uploadPath + Path.DirectorySeparatorChar);
    }

    [HttpGet("generate-dispatch")]
    public async Task<ActionResult<string>> GenerateDispatchId(Guid truckId, string date, CancellationToken cancellationToken)
    {
        var truck = await _context.Trucks.FindAsync([truckId], cancellationToken);
        if (truck == null)
        {
            return NotFound("Truck not found");
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
        {
            return BadRequest("Invalid date format. Expected yyyy-MM-dd");
        }
        
        var baseDispatchId = (parsedDate.ToString("yyMMdd") + truck.LicensePlate?.Substring(2, 6)).Trim();
        Console.WriteLine($"Base DispatchId: {baseDispatchId}");

        // Query existing dispatch ids that start with the base prefix
        var sameDayDispatches = await _context.Trips
            .Where(t => t.TruckId == truckId && EF.Functions.ILike(t.DispatchId, $"{baseDispatchId}%"))
            .Select(t => t.DispatchId)
            .ToListAsync(cancellationToken);

        var existing = sameDayDispatches
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existing.Contains(baseDispatchId))
        {
            return Ok(baseDispatchId);
        }

        int suffix = 1;
        string candidate;
        do
        {
            candidate = $"{baseDispatchId}-{suffix}";
            suffix++;
        } while (existing.Contains(candidate));

        return Ok(candidate);
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
            .Include(x => x.CreatedBy)
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
        var report = TripMapper.ToExportDto(trips, await GetExcludeCngSettingAsync(cancellationToken));

        var csv = new StringBuilder();

        csv.AppendLine(
            "S/N,Dispatch Date,Dispatch ID,Loading Depot Arrival Date,Loading Date,Truck Number,Product,Trip Status,Loading Point,Waybill Number,Dispatch Quantity,Driver Name,Destination,E-lock Status,Dispatch Type,Arrived Depot,Depot Arrival Date,Depot Name,Invoiced,Invoice Date,Arrived Station,Station Arrival Date,Discharging/Discharged?,Discharged Date,Discharge Location,Discharged Quantity,Unit (SCM/KG/MT/LTR),Return Date,Shortage/Overage?,Shortage/Overage Amount,Remarks,Duration Days,Discharge Summary"
        );

        int sn = 1;
        foreach (var s in report)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                sn++.ToString(),
                EscapeCsv(s.Date.ToString("dd/MM/yyyy")),
                EscapeCsv(s.DispatchId),
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

    [HttpPost("report-loading-trips")]
    public async Task<IActionResult> ExportLoadingTripReport([FromBody] ReportFilter request, CancellationToken cancellationToken = default)
    {
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.ReceivingDepot)
            .Include(x => x.Discharges).ThenInclude(x => x.Station)
            .Include(x => x.CreatedBy)
            .Include(x => x.ClosedBy)
            .Include(x => x.CompletedBy)
            .Where(x => x.LoadingInfo.LoadingDate.HasValue && x.LoadingInfo.LoadingDate.Value.Month == request.StartDate.Month && x.LoadingInfo.LoadingDate.Value.Year == request.StartDate.Year)
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
        var report = TripMapper.ToExportDto(trips, await GetExcludeCngSettingAsync(cancellationToken));

        var csv = new StringBuilder();
        
        csv.AppendLine(
            "S/N,Dispatch Date,Dispatch ID,Loading Depot Arrival Date,Loading Date,Truck Number,Product,Trip Status,Loading Point,Waybill Number,Dispatch Quantity,Driver Name,Destination,E-lock Status,Dispatch Type,Arrived Depot,Depot Arrival Date,Depot Name,Invoiced,Invoice Date,Arrived Station,Station Arrival Date,Discharging/Discharged?,Discharged Date,Discharge Location,Discharged Quantity,Unit (SCM/KG/MT/LTR),Return Date,Shortage/Overage?,Shortage/Overage Amount,Remarks,Duration Days,Discharge Summary"
        );

        int sn = 1;
        foreach (var s in report)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                sn++.ToString(),
                EscapeCsv(s.Date.ToString("dd/MM/yyyy")),
                EscapeCsv(s.DispatchId),
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
            return await _context.Trips.Include(x => x.Discharges).Include(x => x.Truck).Where(x =>  x.Truck.Product != Shared.Enums.Product.CngAbuja && x.Truck.Product != Shared.Enums.Product.CngLagos && x.Date >= filter.StartDate.ToDateTime(TimeOnly.MinValue) && x.Date <= filter.EndDate.Value.ToDateTime(TimeOnly.MinValue)).ToArrayAsync(cancellationToken);
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // Shared by the three trip-level drill-down reports below (station/truck/driver-report). A
    // trip's shortage only counts once one of its discharges is marked final — matching every
    // aggregate report's convention (StationReportService et al.) rather than showing a premature
    // number for a still-in-progress delivery — and CNG trips are zeroed out when the setting
    // says so, same as everywhere else, without hiding the row (the drill-down is about this
    // specific truck/driver/station's full history, not a product-filtered view).
    private static decimal ShortageAmountFor(Trip trip, decimal loaded, decimal totalDischarged, bool excludeCng)
    {
        var hasFinalDischarge = trip.Discharges?.Any(d => d.IsFinalDischarge) ?? false;
        if (!hasFinalDischarge) return 0;
        if (excludeCng && (trip.Truck?.Product?.IsCng() ?? false)) return 0;
        return loaded - totalDischarged;
    }

    // No caching, matching AppSettingsController's own read pattern — this is a low-traffic
    // settings row, not worth a cache invalidation story yet.
    private async Task<bool> GetExcludeCngSettingAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.AppSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings?.ExcludeCngFromShortage ?? false;
    }

    // POST: api/Trips/station-report — every trip that discharged at the given
    // station. Shortage is trip-wide (loaded qty minus the sum of ALL of that
    // trip's discharges, not just this station's) — see StationReportDto.
    [HttpPost("station-report")]
    public async Task<ActionResult<List<StationReportDto>>> GetStationReport([FromBody] StationReportFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.Discharges)
            .AsSplitQuery()
            .Where(x => x.Discharges.Any(d => d.StationId == filter.StationId))
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Date >= start);
        }
        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Date <= end);
        }

        var trips = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var report = trips.Select(trip =>
        {
            var totalDischarged = trip.Discharges?.Sum(d => d.QuantityDischarged) ?? 0;
            var dischargedAtStation = trip.Discharges?
                .Where(d => d.StationId == filter.StationId)
                .Sum(d => d.QuantityDischarged) ?? 0;
            var loaded = trip.LoadingInfo?.Quantity ?? 0;

            return new StationReportDto
            {
                TripId = trip.Id,
                Date = trip.Date,
                TruckNo = trip.Truck?.TruckNo,
                TruckPlate = trip.Truck?.LicensePlate,
                Product = trip.Truck?.Product?.ToDisplay(),
                DriverName = trip.Driver?.ToString(),
                DriverPhone = trip.Driver?.PhoneNo,
                LoadingDepot = trip.LoadingDepot?.Name,
                LoadingDate = trip.LoadingInfo?.LoadingDate.HasValue == true
                    ? trip.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy")
                    : null,
                DispatchQuantity = loaded,
                DischargedQuantity = dischargedAtStation,
                ShortageAmount = ShortageAmountFor(trip, loaded, totalDischarged, excludeCng),
                Unit = trip.GetUnit(),
                Status = trip.Status.ToString(),
            };
        }).ToList();

        return report;
    }

    // POST: api/Trips/truck-report — every trip made by the given truck. See
    // TruckTripReportDto for why DischargedQuantity here is the trip-wide total
    // rather than scoped to one station (unlike station-report).
    [HttpPost("truck-report")]
    public async Task<ActionResult<List<TruckTripReportDto>>> GetTruckReport([FromBody] TruckTripReportFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.Discharges).ThenInclude(d => d.Station)
            .AsSplitQuery()
            .Where(x => x.TruckId == filter.TruckId)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Date >= start);
        }
        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Date <= end);
        }

        var trips = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var report = trips.Select(trip =>
        {
            var totalDischarged = trip.Discharges?.Sum(d => d.QuantityDischarged) ?? 0;
            var loaded = trip.LoadingInfo?.Quantity ?? 0;
            var stationNames = trip.Discharges?
                .Where(d => d.Station != null)
                .Select(d => d.Station!.Name)
                .Distinct()
                .ToList() ?? [];

            return new TruckTripReportDto
            {
                TripId = trip.Id,
                Date = trip.Date,
                StationNames = stationNames.Count > 0 ? string.Join(", ", stationNames) : null,
                Product = trip.Truck?.Product?.ToDisplay(),
                DriverName = trip.Driver?.ToString(),
                DriverPhone = trip.Driver?.PhoneNo,
                LoadingDepot = trip.LoadingDepot?.Name,
                LoadingDate = trip.LoadingInfo?.LoadingDate.HasValue == true
                    ? trip.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy")
                    : null,
                DispatchQuantity = loaded,
                DischargedQuantity = totalDischarged,
                ShortageAmount = ShortageAmountFor(trip, loaded, totalDischarged, excludeCng),
                Unit = trip.GetUnit(),
                Status = trip.Status.ToString(),
            };
        }).ToList();

        return report;
    }

    // POST: api/Trips/driver-report — every trip made by the given driver. See
    // DriverTripReportDto for why DischargedQuantity here is the trip-wide total
    // rather than scoped to one station (unlike station-report).
    [HttpPost("driver-report")]
    public async Task<ActionResult<List<DriverTripReportDto>>> GetDriverReport([FromBody] DriverTripReportFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Trips
            .Include(x => x.Driver)
            .Include(x => x.Truck)
            .Include(x => x.LoadingDepot)
            .Include(x => x.Discharges).ThenInclude(d => d.Station)
            .AsSplitQuery()
            .Where(x => x.DriverId == filter.DriverId)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.Date >= start);
        }
        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(x => x.Date <= end);
        }

        var trips = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var report = trips.Select(trip =>
        {
            var totalDischarged = trip.Discharges?.Sum(d => d.QuantityDischarged) ?? 0;
            var loaded = trip.LoadingInfo?.Quantity ?? 0;
            var stationNames = trip.Discharges?
                .Where(d => d.Station != null)
                .Select(d => d.Station!.Name)
                .Distinct()
                .ToList() ?? [];

            return new DriverTripReportDto
            {
                TripId = trip.Id,
                Date = trip.Date,
                TruckNo = trip.Truck?.TruckNo,
                TruckPlate = trip.Truck?.LicensePlate,
                StationNames = stationNames.Count > 0 ? string.Join(", ", stationNames) : null,
                Product = trip.Truck?.Product?.ToDisplay(),
                LoadingDepot = trip.LoadingDepot?.Name,
                LoadingDate = trip.LoadingInfo?.LoadingDate.HasValue == true
                    ? trip.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy")
                    : null,
                DispatchQuantity = loaded,
                DischargedQuantity = totalDischarged,
                ShortageAmount = ShortageAmountFor(trip, loaded, totalDischarged, excludeCng),
                Unit = trip.GetUnit(),
                Status = trip.Status.ToString(),
            };
        }).ToList();

        return report;
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
                                      .Include(x => x.CreatedBy)
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
                                .Include(x => x.CreatedBy)
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
    [Authorize(Roles = "Master, Admin, Supervisor, DriverSupervisor, Manager, Monitoring")]
    public async Task<IActionResult> PutTrip(Guid id, Trip trip, CancellationToken cancellationToken)
    {
        if (id != trip.Id)
        {
            return BadRequest();
        }

        var productScopeError = await ValidateProductScopeAsync(trip, cancellationToken);
        if (productScopeError is not null)
        {
            return Forbid();
        }

        var loadingValidation = ValidateLoadingInfo(trip);
        if (loadingValidation is not null && trip.ArrivalInfo is null)
            return BadRequest(loadingValidation);

        _context.Entry(trip).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
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

        // This save may have just added the loading/arrival ullage readings a pending shortage
        // notification was waiting on.
        await _shortageNotificationService.CheckAndNotifyForTripAsync(id);

        return NoContent();
    }

    // PUT: api/Trips/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("update-no-restriction/{id}")]
    [Authorize(Roles = "Master, Admin")]
    public async Task<IActionResult> PutTripNoRestriction(Guid id, Trip trip, CancellationToken cancellationToken)
    {
        if (id != trip.Id)
        {
            return BadRequest();
        }

        var loadingValidation = ValidateLoadingInfo(trip);
        if (loadingValidation is not null)
            return BadRequest(loadingValidation);

        _context.Entry(trip).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
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

        await _shortageNotificationService.CheckAndNotifyForTripAsync(id);

        return NoContent();
    }

    // [HttpGet("get-dispatch")]
    // public async Task<ActionResult<DispatchDetail>> GetDispatchDetail(string id, CancellationToken cancellationToken)
    // {
    //     if (string.IsNullOrEmpty(id))
    //     {
    //         return BadRequest("Invalid dispatch ID format.");
    //     }        
    //     var dispatch = await _context.Trips
    //         .AsNoTracking()
    //         .Where(t => t.DispatchId.Trim() == id.Trim())
    //         .Select(t => new DispatchDetail(
    //             t.LoadingDepot != null ? t.LoadingDepot.Name : "N/A",
    //             t.LoadingInfo.Destination != null ? t.LoadingInfo.Destination! : "N/A",
    //             t.LoadingInfo.LoadingDate.HasValue ? t.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy") : "N/A",
    //             t.Truck != null ? t.Truck.TruckNo : "N/A",
    //             t.Truck != null ? t.Truck.LicensePlate : "N/A"
    //         ))
    //         .FirstOrDefaultAsync(cancellationToken);

    //     if (dispatch == null)
    //     {
    //         return NotFound("Dispatch not found");
    //     }

    //     return Ok(dispatch);
    // }

    // POST: api/Trips
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    // [HttpPost]
    // public async Task<ActionResult<Trip>> PostTrip(Trip trip)
    // {
    //     var dispatchResult = await GenerateDispatchId(trip.TruckId, trip.Date.ToString("yyyy-MM-dd"), CancellationToken.None);
    //     if (dispatchResult.Result is OkObjectResult okResult && okResult.Value is string dispatchId)
    //     {
    //         Console.WriteLine($"Generated DispatchId: {dispatchId}");
    //         trip.DispatchId = dispatchId.Trim();
    //     }
    //     else
    //     {
    //         return BadRequest("Failed to generate DispatchId.");
    //     }
    //     _context.Trips.Add(trip);
    //     await _context.SaveChangesAsync();

    //     return CreatedAtAction("GetTrip", new { id = trip.Id }, trip);
    // }

    // Called by the external Atlantic Dispatch app, which has no user JWT — API-key gated
    // the same way DispatchCheckController is, rather than the class-level [Authorize]
    // (JWT) every other action here uses. [AllowAnonymous] is required to actually bypass
    // that class-level [Authorize]; ApiKeyAuthFilter is what enforces the real check.
    [HttpGet("get-dispatch")]
    [AllowAnonymous]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public async Task<ActionResult<DispatchDetail>> GetDispatchDetail(string id, CancellationToken cancellationToken)
    {
        // Same shape as DispatchCheckController.DispatchIdRegex — validated here too now that
        // this endpoint is reachable by an external, API-key-authenticated caller rather than
        // only logged-in internal users.
        if (string.IsNullOrWhiteSpace(id) || id.Length > 30 || !DispatchIdRegex.IsMatch(id))
        {
            return BadRequest("Invalid dispatch ID format.");
        }
        var dispatch = await _context.Trips
            .AsNoTracking()
            .Where(t => t.DispatchId.Trim() == id.Trim())
            .Select(t => new DispatchDetail(
                t.LoadingDepot != null ? t.LoadingDepot.Name : "N/A",
                t.LoadingInfo.Destination != null ? t.LoadingInfo.Destination! : "N/A",
                t.LoadingInfo.LoadingDate.HasValue ? t.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy") : "N/A",
                t.Truck != null ? t.Truck.TruckNo : "N/A",
                t.Truck != null ? t.Truck.LicensePlate : "N/A"
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (dispatch == null)
        {
            return NotFound("Dispatch not found");
        }

        return Ok(dispatch);
    }

    [HttpPost("lpg-trips")]
    public async Task<ActionResult<IEnumerable<LpgTripDetail>>> GetLpgTrips([FromBody] ReportFilter request, CancellationToken cancellationToken)
    {
        var query = _context.Trips
            .AsNoTracking()
            .Include(x => x.LoadingDepot)
            .Include(x => x.Truck)
            .Where(t => t.Truck != null && t.Truck.Product == Shared.Enums.Product.LPG
                && t.Date.Month == request.StartDate.Month
                && t.Date.Year == request.StartDate.Year);

        if (request.EndDate.HasValue)
        {
            var endDateTime = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(t => t.Date <= endDateTime);
        }

        var trips = await query.OrderByDescending(t => t.LoadingInfo.LoadingDate).ToListAsync(cancellationToken);

        var result = trips.Select(t =>
        {
            var gross = t.LoadingInfo?.Metrics?.Sum(m => m.GrossWeight ?? 0);
            var tare = t.LoadingInfo?.Metrics?.Sum(m => m.TareWeight ?? 0);
            return new LpgTripDetail(
                t.DispatchId ?? "N/A",
                t.LoadingInfo?.LoadingDate.HasValue == true ? t.LoadingInfo.LoadingDate.Value.ToString("dd/MM/yyyy") : "N/A",
                t.LoadingDepot?.Name ?? "N/A",
                t.LoadingInfo?.Destination ?? "N/A",
                t.Truck?.TruckNo ?? "N/A",
                t.LoadingInfo?.WaybillNo ?? "N/A",
                gross,
                tare,
                gross - tare
            );
        });

        return Ok(result);
    }

    [HttpPost("download-loading-files")]
    [Authorize(Roles = "Admin, Master, Manager")]
    public async Task<IActionResult> DownloadLoadingFilesAsync(
        [FromBody] ReportFilter filter,
        CancellationToken cancellationToken)    
    {
        _logger.LogInformation(
            "Download loading files — Product: {Product}, Start: {Start}, End: {End}",
            filter.Product ?? "All", filter.StartDate, filter.EndDate);

        var query = _context.Trips.AsNoTracking().AsQueryable();    
        query = query.Where(t => t.Date >= filter.StartDate.ToDateTime(TimeOnly.MinValue));

        if (filter.EndDate.HasValue)
        {
            var endDateTime = filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(t => t.Date <= endDateTime);
        }

        Guid[]? productTruckIds = null;
        if (!string.IsNullOrWhiteSpace(filter.Product) && Enum.TryParse<Product>(filter.Product, true, out var parsedProduct))
        {
            productTruckIds = await _context.Trucks
                .AsNoTracking()
                .Where(tr => tr.Product == parsedProduct)
                .Select(tr => tr.Id)
                .ToArrayAsync(cancellationToken);

            query = query.Where(t => productTruckIds.Contains(t.TruckId));
        }

        var tripFiles = await query
            .OrderByDescending(t => t.LoadingInfo.LoadingDate)
            .Select(t => new
            {
                t.Id,
                t.DispatchId,
                t.LoadingInfo
            })
            .ToListAsync(cancellationToken);

        var tripsWithFiles = tripFiles
            .Where(t => t.LoadingInfo?.Files?.Count > 0)
            .ToList();

        if (tripsWithFiles.Count == 0)
            return NotFound("No loading files found for the given criteria.");

        var fileCount = 0;
        var zipStream = new MemoryStream();

        try
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var trip in tripsWithFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var prefix = !string.IsNullOrWhiteSpace(trip.LoadingInfo!.WaybillNo)
                        ? SanitizeFileName(trip.LoadingInfo.WaybillNo)
                        : SanitizeFileName(trip.DispatchId ?? trip.Id.ToString("N"));

                    foreach (var file in trip.LoadingInfo.Files)
                    {
                        if (string.IsNullOrWhiteSpace(file.ServerFileName))
                            continue;

                        // ServerFileName is stored data, not necessarily a value this server generated
                        // (older rows, or a future bug, could carry a path-traversal payload) — resolve
                        // and re-verify it lands inside the upload root before ever opening it.
                        var physicalPath = Path.GetFullPath(Path.Combine(_uploadRoot, file.ServerFileName));
                        if (!physicalPath.StartsWith(_uploadRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Rejected out-of-root file reference: {Name} (Trip: {TripId})", file.ServerFileName, trip.Id);
                            continue;
                        }

                        if (!System.IO.File.Exists(physicalPath))
                        {
                            _logger.LogWarning("File not on disk: {Path} (Trip: {TripId})", physicalPath, trip.Id);
                            continue;
                        }

                        var entryName = EnsureUniqueEntry(addedEntries, $"{prefix}_{file.FileName}");

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        using var fileStream = System.IO.File.OpenRead(physicalPath);
                        await fileStream.CopyToAsync(entryStream, cancellationToken);
                        fileCount++;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build loading files ZIP");
            return StatusCode(500, "Failed to create download archive.");
        }

        if (zipStream.Length == 0)
            return NotFound("No loading files found for the given criteria.");

        zipStream.Position = 0;

        var productLabel = !string.IsNullOrWhiteSpace(filter.Product) ? filter.Product : "All";
        var dateLabel = filter.StartDate != default
            ? $"_{filter.StartDate:yyyy-MM-dd}" + (filter.EndDate != default ? $"_to_{filter.EndDate:yyyy-MM-dd}" : "")
            : "_all";

        _logger.LogInformation(
            "Loading files ZIP ready — {FileCount} files, {Size:N2} MB",
            fileCount, zipStream.Length / (1024.0 * 1024.0));

        return File(zipStream, "application/zip", $"{productLabel}_LoadingFiles{dateLabel}.zip");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    private static string EnsureUniqueEntry(HashSet<string> entries, string candidate)
    {
        if (entries.Add(candidate))
            return candidate;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(candidate);
        var ext = Path.GetExtension(candidate);
        var counter = 1;
        string newName;
        do
        {
            newName = $"{nameWithoutExt}({counter}){ext}";
            counter++;
        } while (!entries.Add(newName));

        return newName;
    }

    // POST: api/Trips
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    [Authorize(Roles = "Master, Admin, Supervisor, DriverSupervisor, Manager, Monitoring")]
    public async Task<ActionResult<Trip>> PostTrip(Trip trip, CancellationToken cancellationToken)
    {
        // Unconditional — a truck with an open trip can never be dispatched again through this
        // endpoint, not even by Admin/Master. There is deliberately no role override here: the
        // override is only for backdating (ValidateDispatchDateAsync below), never for this.
        var openTripError = await ValidateNoOpenTripAsync(trip, cancellationToken);
        if (openTripError is not null)
        {
            return Conflict(openTripError);
        }

        var productScopeError = await ValidateProductScopeAsync(trip, cancellationToken);
        if (productScopeError is not null)
        {
            return Forbid();
        }

        var dispatchValidationError = await ValidateDispatchDateAsync(trip, cancellationToken);
        if (dispatchValidationError is not null)
        {
            return BadRequest(dispatchValidationError);
        }

        // When creating a trip we do not require the Destination field to be populated.
        // Preserve stricter validation for updates (Put). This keeps creation flexible
        // while ensuring existing update flows still validate the presence of Destination
        // when DestinationMode is Single.
        var loadingValidation = ValidateLoadingInfo(trip, requireDestination: false);
        if (loadingValidation is not null)
            return BadRequest(loadingValidation);
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var dispatchResult = await GenerateDispatchId(trip.TruckId, trip.Date.ToString("yyyy-MM-dd"), cancellationToken);
            if (dispatchResult.Result is OkObjectResult okResult && okResult.Value is string dispatchId)
            {
                trip.DispatchId = dispatchId.Trim();
            }
            else
            {
                return BadRequest("Failed to generate DispatchId.");
            }

            // Ensure a new GUID id for the new entity if not provided
            if (trip.Id == Guid.Empty)
                trip.Id = Guid.NewGuid();

            _context.Trips.Add(trip);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return CreatedAtAction("GetTrip", new { id = trip.Id }, trip);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505" && pg.ConstraintName == "UX_Trips_TruckId_OpenStatus")
            {
                // Last-resort race guard: two near-simultaneous requests both passed
                // ValidateNoOpenTripAsync before either committed. The DB constraint is what
                // actually stops the second one — this is not retryable, the truck genuinely
                // already has an open trip now.
                _context.ChangeTracker.Clear();
                _logger?.LogWarning(ex, "Blocked duplicate open trip for truck {TruckId} (race past app-level check).", trip.TruckId);
                return Conflict(new { error = "This truck already has an open trip. Please refresh and try again." });
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
            {
                // Unique violation - likely a race when two requests generate the same DispatchId.
                // Clear change tracker/state and retry generating a new dispatch id.
                _context.ChangeTracker.Clear();
                _logger?.LogWarning(ex, "DispatchId collision for truck {TruckId} dispatch {DispatchId} (attempt {Attempt}/{Max}).", trip.TruckId, trip.DispatchId, attempt, maxAttempts);
                if (attempt == maxAttempts)
                {
                    return Conflict(new { error = "Failed to create trip due to dispatch id collision. Please retry." });
                }
                // otherwise loop to retry
            }
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create trip." });
    }

    // DELETE: api/Trips/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Master, Admin")]
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

    private string? ValidateLoadingInfo(Trip trip)
    {
        // Keep the original single-parameter method for backward compatibility
        // (tools or callers using reflection). Default behavior requires Destination.
        return ValidateLoadingInfo(trip, requireDestination: true);
    }

    private string? ValidateLoadingInfo(Trip trip, bool requireDestination)
    {
        if (trip?.LoadingInfo is null)
            return null;

        if (requireDestination && trip.LoadingInfo.DestinationMode == DestinationMode.Single && string.IsNullOrWhiteSpace(trip.LoadingInfo.Destination))
            return "Destination is required when DestinationMode is Single.";

        return null;
    }

    // A truck already out on an open trip (Active, Dispatched, or Overdue — anything not yet
    // Closed/Completed) can never be dispatched again until that trip is closed. Deliberately no
    // role bypass: this is the hard rule that stops a truck from being dispatched multiple times
    // at once, which the date-ordering check alone never guarded against. Backed by a filtered
    // unique index (see AppDbContext) as a last-resort guard against two near-simultaneous
    // requests both passing this check before either commits.
    private async Task<string?> ValidateNoOpenTripAsync(Trip trip, CancellationToken cancellationToken)
    {
        var openTrip = await _context.Trips
            .AsNoTracking()
            .Where(x => x.TruckId == trip.TruckId && x.Id != trip.Id &&
                        (x.Status == TripStatus.Active || x.Status == TripStatus.Dispatched || x.Status == TripStatus.Overdue))
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (openTrip is null)
            return null;

        return $"This truck already has an open trip (dispatched {openTrip.Date:MMM dd, yyyy}, {openTrip.Status}). " +
               "It must be closed before the truck can be dispatched again.";
    }

    // DriverSupervisor accounts are scoped to a subset of products in the UI, but nothing
    // server-side enforced that — the ManagedProducts claim (Security.GetManagedProducts) was
    // defined but never actually checked by any controller, so a DriverSupervisor calling this
    // API directly could dispatch or edit a trip for a truck outside their assigned products.
    private async Task<string?> ValidateProductScopeAsync(Trip trip, CancellationToken cancellationToken)
    {
        if (!User.IsDriverSupervisor())
            return null;

        var truckProduct = await _context.Trucks
            .AsNoTracking()
            .Where(t => t.Id == trip.TruckId)
            .Select(t => (Product?)t.Product)
            .FirstOrDefaultAsync(cancellationToken);

        if (truckProduct is null)
            return "Truck not found.";

        var managedProducts = User.GetManagedProducts();
        if (!managedProducts.Contains(truckProduct.Value))
        {
            return "You are not authorized to dispatch or edit trips for this product.";
        }

        return null;
    }

    private async Task<string?> ValidateDispatchDateAsync(Trip trip, CancellationToken cancellationToken)
    {
        // Only Admin/Master can back-date a dispatch (e.g. to record a trip that happened but was
        // never entered) — deliberately narrower than the open-trip check above, which has no
        // override for anyone.
        if (User.IsInRole(UserRole.Admin) || User.IsInRole(UserRole.Master))
        {
            return null;
        }

        var latestTrip = await _context.Trips
            .AsNoTracking()
            .Where(x => x.TruckId == trip.TruckId && x.Id != trip.Id)
            .OrderByDescending(x => x.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTrip is null)
        {
            return null;
        }

        var minimumDispatchDate = latestTrip.Date.Date;
        if (latestTrip.CloseInfo.ReturnDateTime.HasValue)
        {
            var returnDate = latestTrip.CloseInfo.ReturnDateTime.Value.Date;
            if (returnDate > minimumDispatchDate)
            {
                minimumDispatchDate = returnDate;
            }
        }

        if (trip.Date.Date < minimumDispatchDate)
        {
            return $"Dispatch date cannot be earlier than {minimumDispatchDate:MMM dd, yyyy}. Only Admin users can back-date dispatches.";
        }

        return null;
    }
}
