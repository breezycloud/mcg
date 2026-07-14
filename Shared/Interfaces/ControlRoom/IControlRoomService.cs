using Shared.Models.ControlRoom;

namespace Shared.Interfaces.ControlRoom;

public interface IControlRoomService
{
    // startDate = null means "All Time" (from inception, no lower bound).
    Task<ControlRoomMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<ProductBreakdownDto>> GetProductBreakdownAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<ProductLeaderDto>> GetProductLeadersAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<ProductLeaderDto>> GetProductLaggardsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default);
    Task<List<RecentIncidentDto>> GetRecentIncidentsAsync(int count = 8, CancellationToken cancellationToken = default);
}
