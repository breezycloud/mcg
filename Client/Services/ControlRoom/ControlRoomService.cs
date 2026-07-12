using System.Net.Http.Json;
using Shared.Interfaces.ControlRoom;
using Shared.Models.ControlRoom;

namespace Client.Services.ControlRoom;

public class ControlRoomService(IHttpClientFactory _httpClient) : IControlRoomService
{
    public async Task<ControlRoomMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"controlroom/metrics?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ControlRoomMetricsDto>(cancellationToken) ?? new();
    }

    public async Task<List<ProductBreakdownDto>> GetProductBreakdownAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"controlroom/product-breakdown?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductBreakdownDto>>(cancellationToken) ?? [];
    }

    public async Task<List<ProductLeaderDto>> GetProductLeadersAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"controlroom/product-leaders?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductLeaderDto>>(cancellationToken) ?? [];
    }

    public async Task<List<ProductLeaderDto>> GetProductLaggardsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"controlroom/product-laggards?startDate={startDate}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductLeaderDto>>(cancellationToken) ?? [];
    }

    public async Task<List<RecentIncidentDto>> GetRecentIncidentsAsync(int count = 8, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"controlroom/recent-incidents?count={count}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<RecentIncidentDto>>(cancellationToken) ?? [];
    }
}
