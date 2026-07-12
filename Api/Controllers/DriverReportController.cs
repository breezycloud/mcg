using Api.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Drivers;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supervisor, Admin, Master, Nrl, DriverSupervisor, Manager")]
public class DriverReportController : ControllerBase
{
    private readonly IDriverReportService _driverReportService;
    private readonly ILogger<DriverReportController> _logger;

    public DriverReportController(IDriverReportService driverReportService, ILogger<DriverReportController> logger)
    {
        _driverReportService = driverReportService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _driverReportService.GetMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver fleet report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetDriverPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _driverReportService.GetDriverPerformanceAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver performance table");
            return StatusCode(500, "Error retrieving driver performance");
        }
    }

    [HttpGet("license-expiry")]
    public async Task<IActionResult> GetLicenseExpiry([FromQuery] int withinDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _driverReportService.GetLicenseExpiryAsync(withinDays, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching license expiry list");
            return StatusCode(500, "Error retrieving license expiry");
        }
    }

    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _driverReportService.GetMonthlyTrendAsync(months, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching driver monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }
}
