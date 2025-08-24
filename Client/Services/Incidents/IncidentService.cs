using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Incidents;
using Shared.Models.Incidents;

namespace Client.Services.Incidents;

public class IncidentService(IHttpClientFactory _httpClient) : IIncidentService
{
    public async Task<bool> AddAsync(Incident model, CancellationToken cancellationToken)
    {
        try
        {
            model.Truck = null; model.Driver = null; model.Trip = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("incidents", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(Incident model, CancellationToken cancellationToken)
    {
        try
        {
            model.Truck = null; model.Driver = null; model.Trip = null;
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"incidents/{model.Id}", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<bool> AddHistoryAsync(IncidentHistory model, CancellationToken cancellationToken)
    {
        try
        {
            model.Incident = null; model.ChangedBy = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"incidents/history", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    public async Task<bool> DeleteHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"incidents/history/delete/{id}", cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"incidents/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<Incident?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"incidents/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Incident?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<GridDataResponse<Incident>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("incidents/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<Incident>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }    
}