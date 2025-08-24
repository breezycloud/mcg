using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Incidents;
using Shared.Models.Incidents;

namespace Client.Services.incidenttypes;

public class IncidentTypeService(IHttpClientFactory _httpClient) : IIncidentTypeService
{
    public async Task<bool> AddAsync(IncidentType model, CancellationToken cancellationToken)
    {
        try
        {          
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("incidenttypes", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(IncidentType model, CancellationToken cancellationToken)
    {
        try
        {            
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"incidenttypes/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"incidenttypes/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<IncidentType?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"incidenttypes/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IncidentType?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<IncidentType[]?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"incidenttypes", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IncidentType[]?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<GridDataResponse<IncidentType>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("incidenttypes/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<IncidentType>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }    
}