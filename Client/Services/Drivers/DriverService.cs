using System.Net.Http.Json;
using Shared.Dtos;
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"drivers/{id}", cancellationToken);
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
    
    public async Task<Driver[]?> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"drivers", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Driver[]?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<Guid?> ValidatePhoneAsync(string phone, Guid? excludeId, CancellationToken cancellationToken)
    {
        var url = $"drivers/validate?phone={Uri.EscapeDataString(phone)}";
        if (excludeId.HasValue) url += $"&excludeId={excludeId.Value}";
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PhoneValidationResult>(cancellationToken: cancellationToken);
        return result?.MatchedId;
    }

    public async Task<DriverImportPreviewResponse?> PreviewImportAsync(byte[] csvContent, string fileName, CancellationToken cancellationToken)
    {
        using var content = BuildCsvContent(csvContent, fileName);
        using var response = await _httpClient.CreateClient("AppUrl").PostAsync("drivers/import/preview", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DriverImportPreviewResponse?>(cancellationToken: cancellationToken);
    }

    public async Task<DriverImportCommitResponse?> CommitImportAsync(byte[] csvContent, string fileName, CancellationToken cancellationToken)
    {
        using var content = BuildCsvContent(csvContent, fileName);
        using var response = await _httpClient.CreateClient("AppUrl").PostAsync("drivers/import/commit", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DriverImportCommitResponse?>(cancellationToken: cancellationToken);
    }

    private static MultipartFormDataContent BuildCsvContent(byte[] csvContent, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(csvContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}