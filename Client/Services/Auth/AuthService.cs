using System.Net.Http.Json;
using Microsoft.JSInterop;
using Shared.Helpers;
using Shared.Interfaces.Auth;
using Shared.Models.Auth;

namespace Client.Services.Auth;

public class AuthService(IHttpClientFactory _httpClient, IJSRuntime js) : IAuthService
{
    public async Task<LoginResponse?> Login(LoginModel login, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient(Constants.Url).PostAsJsonAsync("auth/login", login, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<LoginResponse?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<bool> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient(Constants.Url).PostAsJsonAsync("auth/forgot-password", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<bool> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient(Constants.Url).PostAsJsonAsync("auth/reset-password", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<LoginResponse?> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.CreateClient(Constants.Url).PostAsJsonAsync("auth/change-password", model, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse?>();
    }
}