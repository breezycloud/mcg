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
            Console.WriteLine("{0} {1}", _httpClient, cancellationToken);                    
            using var response = await _httpClient.CreateClient(Constants.Url).PostAsJsonAsync("auth/login", login, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<LoginResponse?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}