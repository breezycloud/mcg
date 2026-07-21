using Api.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces.ControlRoom;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ControlRoomController : ControllerBase
{
    private readonly IControlRoomService _controlRoomService;
    private readonly ILogger<ControlRoomController> _logger;

    public ControlRoomController(IControlRoomService controlRoomService, ILogger<ControlRoomController> logger)
    {
        _controlRoomService = controlRoomService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _controlRoomService.GetMetricsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching control room metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    [HttpGet("product-breakdown")]
    public async Task<IActionResult> GetProductBreakdown(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _controlRoomService.GetProductBreakdownAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching control room product breakdown");
            return StatusCode(500, "Error retrieving product breakdown");
        }
    }

    [HttpGet("product-leaders")]
    public async Task<IActionResult> GetProductLeaders(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _controlRoomService.GetProductLeadersAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching control room product leaders");
            return StatusCode(500, "Error retrieving product leaders");
        }
    }

    [HttpGet("product-laggards")]
    public async Task<IActionResult> GetProductLaggards(
        [FromQuery, ModelBinder(typeof(MultiDateFormatBinder))] DateOnly? startDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _controlRoomService.GetProductLaggardsAsync(startDate, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching control room product laggards");
            return StatusCode(500, "Error retrieving product laggards");
        }
    }

    [HttpGet("recent-incidents")]
    public async Task<IActionResult> GetRecentIncidents([FromQuery] int count = 8, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _controlRoomService.GetRecentIncidentsAsync(count, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching control room recent incidents");
            return StatusCode(500, "Error retrieving recent incidents");
        }
    }
}
