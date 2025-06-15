using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Drivers;
using Shared.Models.Drivers;

namespace Client.Services.Drivers;


public class DriverService(IHttpClientFactory _httpClient) : IDriverService
{
    public async Task<bool> AddAsync(Driver model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("drivers", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(Driver model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"drivers/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"drivers/{id}",  cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<Driver?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"drivers/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Driver?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<GridDataResponse<Driver>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("drivers/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<Driver>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}