using System.Net.Http.Json;
using Shared.Interfaces.Settings;
using Shared.Models.Settings;

namespace Client.Services.Settings;

public class AppSettingsService(IHttpClientFactory _httpClient) : IAppSettingsService
{
    public async Task<NotificationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").GetAsync("AppSettings", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationSettings>(cancellationToken) ?? new NotificationSettings();
    }

    public async Task<bool> UpdateAsync(NotificationSettings settings, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync("AppSettings", settings, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
