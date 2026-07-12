using Api.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.Stations;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Supervisor, Admin, Master, Nrl, DriverSupervisor, Manager")]
public class StationReportController : ControllerBase
{
    private readonly IStationReportService _stationReportService;
    private readonly ILogger<StationReportController> _logger;

    public StationReportController(IStationReportService stationReportService, ILogger<StationReportController> logger)
    {
        _stationReportService = stationReportService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching station fleet report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> GetStationPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetStationPerformanceAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching station performance table");
            return StatusCode(500, "Error retrieving station performance");
        }
    }

    [HttpGet("monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _stationReportService.GetMonthlyTrendAsync(months, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching station monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }

    [HttpGet("loading-depot/metrics")]
    public async Task<IActionResult> GetLoadingDepotMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetLoadingDepotMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loading depot report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("loading-depot/performance")]
    public async Task<IActionResult> GetLoadingDepotPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetLoadingDepotPerformanceAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loading depot performance table");
            return StatusCode(500, "Error retrieving loading depot performance");
        }
    }

    [HttpGet("loading-depot/monthly-trend")]
    public async Task<IActionResult> GetLoadingDepotMonthlyTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _stationReportService.GetLoadingDepotMonthlyTrendAsync(months, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching loading depot monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }

    [HttpGet("receiving-depot/metrics")]
    public async Task<IActionResult> GetReceivingDepotMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetReceivingDepotMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receiving depot report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("receiving-depot/performance")]
    public async Task<IActionResult> GetReceivingDepotPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetReceivingDepotPerformanceAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receiving depot performance table");
            return StatusCode(500, "Error retrieving receiving depot performance");
        }
    }

    [HttpGet("receiving-depot/monthly-trend")]
    public async Task<IActionResult> GetReceivingDepotMonthlyTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _stationReportService.GetReceivingDepotMonthlyTrendAsync(months, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching receiving depot monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }

    [HttpGet("refuelling-station/metrics")]
    public async Task<IActionResult> GetRefuellingStationMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetRefuellingStationMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refuelling station report metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("refuelling-station/performance")]
    public async Task<IActionResult> GetRefuellingStationPerformance(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _stationReportService.GetRefuellingStationPerformanceAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refuelling station performance table");
            return StatusCode(500, "Error retrieving refuelling station performance");
        }
    }

    [HttpGet("refuelling-station/monthly-trend")]
    public async Task<IActionResult> GetRefuellingStationMonthlyTrend([FromQuery] int months = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _stationReportService.GetRefuellingStationMonthlyTrendAsync(months, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refuelling station monthly trend");
            return StatusCode(500, "Error retrieving monthly trend");
        }
    }
}
