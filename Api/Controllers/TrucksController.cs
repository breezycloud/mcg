using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Api.Context;
using Api.Services.Discharges;
using Shared.Dtos;
using Shared.Models.Trucks;
using Shared.Helpers;
using Shared.Enums;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TrucksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ShortageNotificationService _shortageNotificationService;

    public TrucksController(AppDbContext context, ShortageNotificationService shortageNotificationService)
    {
        _context = context;
        _shortageNotificationService = shortageNotificationService;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Truck>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Truck> response = new();
        try
        {
            var query = _context.Trucks
                .AsNoTracking()
                .Include(x => x.Driver)
                .Include(x => x.ServiceRequests)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.Driver.FirstName, pattern) || EF.Functions.ILike(x.Driver.LastName, pattern) || EF.Functions.ILike(x.TruckNo, pattern) || EF.Functions.ILike(x.Manufacturer!, pattern) || EF.Functions.ILike(x.VIN!, pattern) || EF.Functions.ILike(x.LicensePlate!, pattern) || EF.Functions.ILike(x.EngineNo!, pattern));
            }

            if (request.UnassignedOnly)
            {
                query = query.Where(x => x.DriverId == null);
            }

            response.Total = await query.CountAsync(cancellationToken);
            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.UpdatedAt).Skip(request.Paging).Take(request.PageSize).AsAsyncEnumerable().WithCancellation(cancellationToken);

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

    // POST: api/Paged
    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<Truck>?>> GetPagedDatAsync(string state, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Truck> response = new();
        try
        {
            IQueryable<Truck> truckQuery = GetDispatchEligibleTrucksQuery();

            // // Optionally filter by state if provided
            // if (!string.IsNullOrEmpty(state))
            // {
            //     string pattern = $"%{state}%";
            //     truckQuery = truckQuery.Where(x => EF.Functions.ILike(x.Status.ToString(), pattern));
            // }

            response.Total = await truckQuery.CountAsync(cancellationToken);

            response.Data = [];
            var pagedQuery = truckQuery.OrderBy(x => x.LicensePlate).AsAsyncEnumerable().WithCancellation(cancellationToken);

            await foreach (var item in pagedQuery)
            {
                response.Data.Add(item);
            }

            return response.Data.ToArray();
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    [HttpGet("available-trucks")]
    public async Task<ActionResult<IEnumerable<Truck>?>> GetAvailableTrucksAsync(string product, CancellationToken cancellationToken = default)
    {
        var truckQuery = GetDispatchEligibleTrucksQuery();

        // Filter by product if provided
        if (!string.IsNullOrEmpty(product) && product != "All")
        {
            truckQuery = truckQuery.Where(truck => truck.Product.ToString()!.Trim() == product.Trim());
        }

        return await truckQuery.OrderBy(x => x.LicensePlate).ToListAsync(cancellationToken);
    }

    // GET: api/Validate/{type}/value
    [HttpGet("validate")]
    public async Task<ActionResult<bool>> ValidateEntry(string type, string value, CancellationToken cancellationToken = default)
    {
        bool response = false;
        try
        {
            string pattern = $"%{value.Trim()}%";
            if (type == "VIN")
                response = await _context.Trucks.Where(x => EF.Functions.ILike(x.VIN!, pattern)).AnyAsync(cancellationToken);
            else if (type == "EngineNo")
                response = await _context.Trucks.Where(x => EF.Functions.ILike(x.EngineNo!, pattern)).AnyAsync(cancellationToken);
            else if (type == "TruckNo")
                response = await _context.Trucks.Where(x => EF.Functions.ILike(x.TruckNo!, pattern)).AnyAsync(cancellationToken);
            else if (type == "LicensePlate")
                response = await _context.Trucks.Where(x => EF.Functions.ILike(x.LicensePlate!, pattern)).AnyAsync(cancellationToken);

            return response;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    // GET: api/Trucks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Truck>>> GetTrucks()
    {
        return await _context.Trucks.AsNoTracking()
                                    .AsSplitQuery()
                                    .Include(x => x.Trips)
                                    .ThenInclude(x => x.Discharges!)
                                    .Include(x => x.ServiceRequests)
                                    .OrderBy(x => x.LicensePlate)
                                    .ToListAsync();
    }

    // GET: api/Trucks/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Truck>> GetTruck(Guid id)
    {
        var truck = await _context.Trucks.AsNoTracking()
                                         .Include(x => x.Driver)
                                         .Include(x => x.ServiceRequests)
                                         .Include(x => x.Trips)
                                         .AsSplitQuery()
                                         .FirstOrDefaultAsync(x => x.Id == id);

        if (truck == null)
        {
            return NotFound();
        }

        return truck;
    }

    // PUT: api/Trucks/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTruck(Guid id, Truck truck)
    {
        if (id != truck.Id)
        {
            return BadRequest();
        }

        _context.Entry(truck).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TruckExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        // This save may have just added the calibration chart a pending shortage notification
        // was waiting on.
        await _shortageNotificationService.CheckAndNotifyForTruckAsync(id);

        return NoContent();
    }

    [Authorize(Roles = "Supervisor, Admin, Master, DriverSupervisor, Manager")]
    [HttpPut("{id}/driver")]
    public async Task<IActionResult> AssignDriver(Guid id, TruckDriverAssignmentDto model, CancellationToken cancellationToken)
    {
        if (id != model.TruckId)
        {
            return BadRequest("Truck mismatch.");
        }

        var truck = await _context.Trucks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (truck is null)
        {
            return NotFound();
        }

        if (model.DriverId.HasValue)
        {
            var driverExists = await _context.Drivers.AnyAsync(x => x.Id == model.DriverId.Value, cancellationToken);
            if (!driverExists)
            {
                return BadRequest("Selected driver was not found.");
            }
        }

        truck.DriverId = model.DriverId;
        truck.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // POST: api/Trucks
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Truck>> PostTruck(Truck truck, CancellationToken cancellationToken)
    {
        if (truck.Id == Guid.Empty)
            truck.Id = Guid.NewGuid();

        _context.Trucks.Add(truck);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            // Last-resort race guard: two near-simultaneous "Add Truck" submissions both passed
            // client-side uniqueness pre-checks before either committed — same fix pattern as
            // TripsController's dispatch race guard.
            _context.ChangeTracker.Clear();
            var field = pg.ConstraintName switch
            {
                "UX_Trucks_TruckNo" => "truck number",
                "UX_Trucks_LicensePlate" => "license plate",
                "UX_Trucks_VIN" => "VIN",
                _ => "identifying field"
            };
            return Conflict(new { error = $"A truck with this {field} already exists." });
        }

        return CreatedAtAction("GetTruck", new { id = truck.Id }, truck);
    }

    // DELETE: api/Trucks/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTruck(Guid id)
    {
        var truck = await _context.Trucks.FindAsync(id);
        if (truck == null)
        {
            return NotFound();
        }

        _context.Trucks.Remove(truck);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<Truck> GetDispatchEligibleTrucksQuery()
    {
        // Must match TripsController.ValidateNoOpenTripAsync's definition of "open" — a truck
        // sitting in Overdue still has an undispatched trip, it's just running long.
        var trucksWithUnavailableTrips = _context.Trips
            .Where(t => t.Status == TripStatus.Active || t.Status == TripStatus.Dispatched || t.Status == TripStatus.Overdue)
            .Select(t => t.TruckId)
            .Distinct();

        var trucksUnderMaintenance = _context.ServiceRequest
            .Where(request => request.TruckId.HasValue
                && (request.Status == RequestStatus.Pending
                    || request.Status == RequestStatus.InProgress
                    || request.Status == RequestStatus.Escalated))
            .Select(request => request.TruckId!.Value)
            .Distinct();

        return _context.Trucks
            .AsNoTracking()
            .Include(x => x.Driver)
            .Where(truck => truck.IsActive)
            .Where(truck => !trucksWithUnavailableTrips.Contains(truck.Id))
            .Where(truck => !trucksUnderMaintenance.Contains(truck.Id));
    }

    private bool TruckExists(Guid id)
    {
        return _context.Trucks.Any(e => e.Id == id);
    }
}
