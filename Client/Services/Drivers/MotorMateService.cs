using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Drivers;
using Shared.Models.Drivers;

namespace Client.Services.Drivers;

public class MotorMateService(IHttpClientFactory _httpClient) : IMotorMateService
{
    public async Task<bool> AddAsync(MotorMate model, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("motormates", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAsync(MotorMate model, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"motormates/{model.Id}", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"motormates/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return response.IsSuccessStatusCode;
    }

    public async Task<MotorMate?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"motormates/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MotorMate?>(cancellationToken);
    }

    public async Task<MotorMate[]?> GetAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync("motormates", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MotorMate[]?>(cancellationToken);
    }

    public async Task<GridDataResponse<MotorMate>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("motormates/paged", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GridDataResponse<MotorMate>?>(cancellationToken);
    }
}
