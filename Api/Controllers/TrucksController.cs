using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Trucks;
using Shared.Helpers;
using Shared.Enums;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrucksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrucksController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<Truck>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Truck> response = new();
        try
        {
            var query = _context.Trucks.AsQueryable();
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Where(x => EF.Functions.ILike(x.TruckNo, pattern) || EF.Functions.ILike(x.Manufacturer!, pattern)
                || EF.Functions.ILike(x.VIN!, pattern));
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

    // POST: api/Paged
    [HttpGet("status")]
    public async Task<ActionResult<IEnumerable<Truck>?>> GetPagedDatAsync(string state, CancellationToken cancellationToken = default)
    {
        GridDataResponse<Truck> response = new();
        try
        {
            IQueryable<Truck> truckQuery;

            // Get trucks with no active trip (assuming "Active" is the status for active trips)
            var trucksWithActiveTrips = _context.Trips
                .Where(t => t.Status == TripStatus.Active)
                .Select(t => t.TruckId)
                .Distinct();

            truckQuery = _context.Trucks
                .Where(truck => !trucksWithActiveTrips.Contains(truck.Id));

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
    public async Task<ActionResult<IEnumerable<Truck>?>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default)
    {
        GridDataResponse<Truck> response = new();
        try
        {
            IQueryable<Truck> truckQuery;

            // Get trucks with no active trip (assuming "Active" is the status for active trips)
            var trucksWithActiveTrips = _context.Trips
                .Where(t => t.Status == TripStatus.Active || t.Status == TripStatus.Dispatched)
                .Select(t => t.TruckId)
                .Distinct();

            truckQuery = _context.Trucks
                .Where(truck => !trucksWithActiveTrips.Contains(truck.Id));


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

        return NoContent();
    }

    // POST: api/Trucks
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Truck>> PostTruck(Truck truck)
    {
        _context.Trucks.Add(truck);
        await _context.SaveChangesAsync();

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

    private bool TruckExists(Guid id)
    {
        return _context.Trucks.Any(e => e.Id == id);
    }
}
