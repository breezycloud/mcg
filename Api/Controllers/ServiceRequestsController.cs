using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Models.Services;
using Shared.Helpers;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServiceRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServiceRequestsController(AppDbContext context)
    {
        _context = context;
    }

     // POST: api/Paged
    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<ServiceRequest>?>> GetPagedDatAsync(GridDataRequest request, CancellationToken cancellationToken = default)
    {
        GridDataResponse<ServiceRequest> response = new();
        try
        {
            var query = _context.ServiceRequest.Include(x => x.Truck).AsQueryable();

            if (request.Id.HasValue)
            {
                query = query.Where(x => x.MaintenanceSiteId == request.Id.Value);
            }
            
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                string pattern = $"%{request.SearchTerm}%";
                query = query.Include(x => x.Truck).Where(x => EF.Functions.ILike(x.Truck!.TruckNo, pattern));
            }

            

            response.Total = await query.CountAsync(cancellationToken);



            response.Data = [];
            var pagedQuery = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.TreatedAt).Skip(request.Page).Take(request.PageSize).AsAsyncEnumerable();

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


    // GET: api/ServiceRequests
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceRequest>>> GetServiceRequest()
    {
        return await _context.ServiceRequest.ToListAsync();
    }

    // GET: api/ServiceRequests/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceRequest>> GetServiceRequest(Guid id)
    {
        var serviceRequest = await _context.ServiceRequest.AsNoTracking()
                                                          .Include(x => x.Site)
                                                          .Include(x => x.Truck)
                                                          .Include(x => x.CreatedBy)
                                                          .Include(x => x.TreatedBy)
                                                          .Include(x => x.ClosedBy)
                                                          .AsSplitQuery()
                                                          .FirstOrDefaultAsync(x => x.Id == id);

        if (serviceRequest == null)
        {
            return NotFound();
        }

        return serviceRequest;
    }

    // PUT: api/ServiceRequests/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutServiceRequest(Guid id, ServiceRequest serviceRequest)
    {
        if (id != serviceRequest.Id)
        {
            return BadRequest();
        }

        _context.Entry(serviceRequest).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceRequestExists(id))
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

    // POST: api/ServiceRequests
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<ServiceRequest>> PostServiceRequest(ServiceRequest serviceRequest)
    {
        _context.ServiceRequest.Add(serviceRequest);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetServiceRequest", new { id = serviceRequest.Id }, serviceRequest);
    }

    // DELETE: api/ServiceRequests/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServiceRequest(Guid id)
    {
        var serviceRequest = await _context.ServiceRequest.FindAsync(id);
        if (serviceRequest == null)
        {
            return NotFound();
        }

        _context.ServiceRequest.Remove(serviceRequest);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServiceRequestExists(Guid id)
    {
        return _context.ServiceRequest.Any(e => e.Id == id);
    }
}
