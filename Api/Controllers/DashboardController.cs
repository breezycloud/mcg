using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Dashboards;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _dashboardService.GetMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("status-distribution")]
    public async Task<IActionResult> GetStatusDistribution()
    {
        var distribution = await _dashboardService.GetTripStatusDistributionAsync();
        return Ok(distribution);
    }

    [HttpGet("product-shipments")]
    public async Task<IActionResult> GetProductShipments()
    {
        var shipments = await _dashboardService.GetProductShipmentsAsync();
        return Ok(shipments);
    }

    [HttpGet("recent-trips")]
    public async Task<IActionResult> GetRecentTrips([FromQuery] int count = 5)
    {
        var trips = await _dashboardService.GetRecentTripsAsync(count);
        return Ok(trips);
    }
}