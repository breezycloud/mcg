using Api.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Trucks;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supervisor, Admin, Master, Nrl, DriverSupervisor, Manager")]
public class TruckReportController : ControllerBase
{
    private readonly ITruckReportService _truckReportService;
    private readonly ILogger<TruckReportController> _logger;

    public TruckReportController(ITruckReportService truckReportService, ILogger<TruckReportController> logger)
    {
        _truckReportService = truckReportService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        [FromQuery] string? product,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _truckReportService.GetMetricsAsync(startDate, product, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching truck fleet report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("status-breakdown")]
    public async Task<IActionResult> GetStatusBreakdown([FromQuery] string? product, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _truckReportService.GetStatusBreakdownAsync(product, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching truck status breakdown");
            return StatusCode(500, "Error retrieving status breakdown");
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetTruckPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        [FromQuery] string? product,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _truckReportService.GetTruckPerformanceAsync(startDate, product, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching truck performance table");
            return StatusCode(500, "Error retrieving truck performance");
        }
    }

    [HttpGet("maintenance-spend/by-truck")]
    public async Task<IActionResult> GetMaintenanceSpendByTruck(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _truckReportService.GetMaintenanceSpendByTruckAsync(startDate, count, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance spend by truck");
            return StatusCode(500, "Error retrieving maintenance spend");
        }
    }

    [HttpGet("maintenance-spend/by-category")]
    public async Task<IActionResult> GetMaintenanceSpendByCategory(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _truckReportService.GetMaintenanceSpendByCategoryAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching maintenance spend by category");
            return StatusCode(500, "Error retrieving maintenance spend");
        }
    }

    [HttpGet("calibration-expiry")]
    public async Task<IActionResult> GetCalibrationExpiry([FromQuery] int withinDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _truckReportService.GetCalibrationExpiryAsync(withinDays, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching calibration expiry list");
            return StatusCode(500, "Error retrieving calibration expiry");
        }
    }

    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend(
        [FromQuery] int months = 6,
        [FromQuery] string? product = "All",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _truckReportService.GetMonthlyTrendAsync(months, product, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fleet monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }
}
