using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.TripCheckpoints;
using Shared.Interfaces.TripCheckpoints;
using System.Net.Http.Json;

namespace Client.Services.TripCheckpoints;

public class TripCheckpointService(IHttpClientFactory _httpClient) : ITripCheckpointService
{
    public async Task<bool> AddAsync(TripCheckpoint model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("TripCheckpoints", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(TripCheckpoint model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"TripCheckpoints/{model.Id}", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"TripCheckpoints/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<TripCheckpoint?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"TripCheckpoints/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TripCheckpoint?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    
    public async Task<GridDataResponse<TripCheckpoint>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"TripCheckpoints/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<TripCheckpoint>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}