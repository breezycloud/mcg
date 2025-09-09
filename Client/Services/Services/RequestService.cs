using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Services;
using Shared.Models.Services;

namespace Client.Services.Services;

public class RequestService(IHttpClientFactory _httpClient) : IRequestService
{
    public async Task<bool> AddAsync(ServiceRequest model, CancellationToken cancellationToken)
    {
        try
        {
            model.Truck = null; model.Driver = null; model.Trip = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("servicerequests", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(ServiceRequest model, CancellationToken cancellationToken)
    {
        try
        {
            model.Truck = null; model.Driver = null; model.Trip = null;
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"servicerequests/{model.Id}", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<bool> AddHistoryAsync(ServiceRequestHistory model, CancellationToken cancellationToken)
    {
        try
        {
            model.ServiceRequest = null; model.ChangedBy = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"servicerequests/history", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"servicerequests/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<ServiceRequest?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"servicerequests/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ServiceRequest?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<GridDataResponse<ServiceRequest>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("servicerequests/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<ServiceRequest>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<GridDataResponse<ServiceRequest>?> ReportPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("servicerequests/paged-report", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<ServiceRequest>?>();
        }
        catch (System.Exception)
        {

            throw;
        }        
    }
}