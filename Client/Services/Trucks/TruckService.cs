using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Models.Trucks;

namespace Shared.Interfaces.Trucks;


public class TruckService(IHttpClientFactory _httpClient) : ITruckService
{
    public async Task<bool> AddAsync(Truck model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("Trucks", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(Truck model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"Trucks/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"Trucks/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<Truck?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Trucks/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Truck?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<GridDataResponse<Truck>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Trucks/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<Truck>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<Truck[]?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync("trucks", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Truck[]?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<Truck[]?> GetTrucksAvailableAsync(string product= "", CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"trucks/available-trucks?product={product}", cancellationToken);        
        return await response.Content.ReadFromJsonAsync<Truck[]?>();
    }


    public async Task<Truck[]?> GetAsync(string status, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"trucks/status?state={status}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Truck[]?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }    

    public async ValueTask<bool> ValidateEntry(string type, string value, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"trucks/validate?type={type}&value={value}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public Task ExportToExcel<T>(List<T> data, string fileName)
    {
        throw new NotImplementedException();
    }

    public Task ExportToPdf<T>(List<T> data, string fileName)
    {
        throw new NotImplementedException();
    }
}