using System.Net.Http.Json;
using Shared.Interfaces.Trips;
using Shared.Models.Trips;

namespace Client.Services.Trips;

public class ShortageRecommendationService(IHttpClientFactory _httpClient) : IShortageRecommendationService
{
    public async Task<List<ShortageRecommendation>> GetForTripAsync(Guid tripId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"ShortageRecommendations/trip/{tripId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ShortageRecommendation>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<bool> AddAsync(ShortageRecommendation model, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("ShortageRecommendations", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }
}
