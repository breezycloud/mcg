using Api.Hubs;
using Api.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Shared.Interfaces.Dashboards;
using Shared.Models.Dashboards;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;
    private readonly IHubContext<DashboardHub> _hubContext;

    public DashboardController(IDashboardService dashboardService,
        ILogger<DashboardController> logger,
        IHubContext<DashboardHub> hubContext)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _hubContext = hubContext;
    }

    #region Metrics Endpoints

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), 200)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    {
        Console.WriteLine("{0} {1}", startDate, endDate);
        try
        {
            var metrics = await _dashboardService.GetMetricsAsync(startDate, endDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("metrics/trends")]
    [ProducesResponseType(typeof(MetricsTrendDto), 200)]
    public async Task<IActionResult> GetMetricsTrends(
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    {
        try
        {
            var trends = await _dashboardService.GetMetricsTrendsAsync(startDate, endDate);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trends");
            return StatusCode(500, "Error calculating trends");
        }
    }

    #endregion

    #region Trip Analytics

    [HttpGet("status-distribution")]
    [ProducesResponseType(typeof(TripStatusDistributionDto), 200)]
    public async Task<IActionResult> GetTripStatusDistribution(
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    {
        try
        {
            var distribution = await _dashboardService.GetTripStatusDistributionAsync(startDate, endDate);
            return Ok(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trip status distribution");
            return StatusCode(500, "Error retrieving status data");
        }
    }

    [HttpGet("product-shipments")]
    [ProducesResponseType(typeof(List<ProductShipmentDto>), 200)]
    public async Task<IActionResult> GetProductShipments(
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    {
        try
        {
            var shipments = await _dashboardService.GetProductShipmentsAsync(startDate, endDate);
            return Ok(shipments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product shipments");
            return StatusCode(500, "Error retrieving shipment data");
        }
    }

    [HttpGet("recent-trips")]
    [ProducesResponseType(typeof(List<RecentTripDto>), 200)]
    public async Task<IActionResult> GetRecentTrips(
         [FromQuery] int count = 5,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate = null,
         [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate = null)
    {
        try
        {
            var trips = await _dashboardService.GetRecentTripsAsync(count, startDate, endDate);
            return Ok(trips);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent trips");
            return StatusCode(500, "Error retrieving trip data");
        }
    }

    #endregion

    // #region Data Import/Export

    // [HttpPost("import/trips")]
    // [ProducesResponseType(typeof(ImportResult), 200)]
    // [ProducesResponseType(400)]
    // public async Task<IActionResult> ImportTrips(IFormFile file)
    // {
    //     if (file == null || file.Length == 0)
    //         return BadRequest("No file uploaded");

    //     if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
    //         return BadRequest("Only .xlsx files are supported");

    //     try
    //     {
    //         using var stream = new MemoryStream();
    //         await file.CopyToAsync(stream);
    //         stream.Position = 0;

    //         var result = await _importService.ImportTripsAsync(stream);

    //         // Notify clients of new data
    //         await _hubContext.Clients.All.SendAsync("ReceiveTripUpdate", 
    //             new { Message = $"{result.SuccessCount} trips imported" });

    //         return Ok(result);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error importing trips");
    //         return StatusCode(500, "Error processing file");
    //     }
    // }

    // [HttpGet("export/trips")]
    // public async Task<IActionResult> ExportTrips(
    //      [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
    //      [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    // {
    //     try
    //     {
    //         var stream = await _exportService.ExportTripsAsync(startDate, endDate);
    //         return File(stream, 
    //             "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
    //             $"TripsExport_{DateOnly.Now:yyyyMMdd}.xlsx");
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error exporting trips");
    //         return StatusCode(500, "Error generating export");
    //     }
    // }

    // [HttpGet("export/product-shipments")]
    // public async Task<IActionResult> ExportProductShipments(
    //      [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
    //      [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? endDate)
    // {
    //     try
    //     {
    //         var stream = await _exportService.ExportProductShipmentsAsync(startDate, endDate);
    //         return File(stream, 
    //             "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
    //             $"ProductShipments_{DateOnly.Now:yyyyMMdd}.xlsx");
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error exporting product shipments");
    //         return StatusCode(500, "Error generating export");
    //     }
    // }

    // #endregion

    #region Real-Time Updates

    [HttpPost("notify-update")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> SendManualUpdate([FromBody] string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTripUpdate", new { Message = message });
        return Ok();
    }

    #endregion
}