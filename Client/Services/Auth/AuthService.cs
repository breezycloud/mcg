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
            return new()
            {
                Id = Guid.Parse("393d652e-71d2-4c22-921e-3e7b5e1c64a3"),
                Email = "nerdyamin@gmail.com",
                Token = "eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTUxMiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJuZXJkeWFtaW5AZ21haWwuY29tIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiTWFzdGVyIiwiZXhwIjoxNzUyNTI3NTIzfQ.dGA0EO9varyzldAQxZeAO6iXztJYxQSuY-keeq0eSA0EsZfsx4H-SK24fv_7TKPsLWyHU0DWzapifz5dhWRg6g",
                Role = 0,                
            };
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}