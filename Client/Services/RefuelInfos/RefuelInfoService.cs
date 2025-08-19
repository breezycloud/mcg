using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.RefuelInfos;
using Shared.Interfaces.RefuelInfos;
using System.Net.Http.Json;

namespace Client.Services.RefuelInfos;

public class RefuelInfoService(IHttpClientFactory _httpClient) : IRefuelInfoService
{
    public async Task<bool> AddAsync(RefuelInfo model, CancellationToken cancellationToken)
    {
        try
        {
            model.Truck = null; model.Station = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("RefuelInfos", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(RefuelInfo model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"RefuelInfos/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"RefuelInfos/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<RefuelInfo?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"RefuelInfos/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RefuelInfo?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    
    public async Task<GridDataResponse<RefuelInfo>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"RefuelInfos/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<RefuelInfo>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}