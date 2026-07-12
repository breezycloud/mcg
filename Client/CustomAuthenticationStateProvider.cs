using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Shared.Models;
using Blazored.LocalStorage;
using Shared.Models.Auth;
using Client.Handlers;
using Client.Services.Messages;
using Microsoft.Extensions.Configuration;
using Shared.Helpers;
using Shared.Interfaces.Auth;

namespace Client
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ILocalStorageService _localStorage;
        private HttpClient _httpClient;
        private NavigationManager _navigation;
        private readonly AppState _appState;
        private readonly AppHubService _hubService;
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;

        public CustomAuthenticationStateProvider(
            ILocalStorageService localStorage,
            HttpClient httpClient,
            NavigationManager navigation,
            AppState appState,
            AppHubService hubService,
            IConfiguration configuration,
            IAuthService authService)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
            _navigation = navigation;
            _appState = appState;
            _hubService = hubService;
            _configuration = configuration;
            _authService = authService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var isConfigured = await _localStorage.GetItemAsync<bool?>("IsConfigured");
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = ParseClaimsFromJwt(token);
            var expiry = claims.Where(claim => claim.Type.Equals("exp")).FirstOrDefault();
            if (expiry == null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            // The exp field is in Unix time
            var datetime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry.Value));
            if (datetime.UtcDateTime <= DateTime.UtcNow)
            {
                // The access token (now 30 minutes) has expired — before giving up, try the
                // refresh token first. This is what makes a page reload/revisit after the access
                // token's lifetime not immediately bounce the user to the login page; the 401-
                // triggered path in CustomAuthorizationHandler covers requests made while a page
                // is already open, this covers the "came back later" case.
                var refreshed = await TryRefreshAsync();
                if (refreshed is null)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                token = refreshed;
                claims = ParseClaimsFromJwt(token);
            }

            //var identity = string.IsNullOrEmpty(token)
            //    ? new ClaimsIdentity()
            //    : new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var managed = Security.GetManagedProducts(principal);
            _appState.SetDriverSupervisorContext(principal.IsDriverSupervisor(), managed);
            return new AuthenticationState(principal);
        }

        // Called by CustomAuthorizationHandler's 401 path too (indirectly, via localStorage) and
        // by GetAuthenticationStateAsync's own expired-token path above — both bypass this method
        // and write "token"/"refreshToken" directly, since neither has a full LoginResponse to
        // hand it and neither needs the SignalR reconnect this method also does. Keep the storage
        // key names in sync across all three if either ever changes.
        private async Task<string?> TryRefreshAsync()
        {
            var refreshToken = await _localStorage.GetItemAsync<string>("refreshToken");
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            try
            {
                var result = await _authService.RefreshToken(refreshToken, CancellationToken.None);
                if (result?.Token is null)
                {
                    await _localStorage.RemoveItemAsync("token");
                    await _localStorage.RemoveItemAsync("refreshToken");
                    return null;
                }

                await _localStorage.SetItemAsync("token", result.Token);
                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    await _localStorage.SetItemAsync("refreshToken", result.RefreshToken);
                }
                return result.Token;
            }
            catch (Exception)
            {
                // Network failure — leave stored tokens alone, this may just be offline.
                return null;
            }
        }

        public async Task SetTokenAsync(LoginResponse response)
        {
            if (string.IsNullOrEmpty(response.Token))
            {
                await _localStorage.RemoveItemAsync("token");
                await _localStorage.RemoveItemAsync("refreshToken");
                await _localStorage.RemoveItemAsync("uid");
                await _localStorage.RemoveItemAsync("role");
                await _localStorage.RemoveItemAsync("shopId");
                // Disconnect SignalR on logout
                await _hubService.StopConnectionAsync();
            }
            else
            {
                await _localStorage.SetItemAsync("token", response.Token);
                if (!string.IsNullOrEmpty(response.RefreshToken))
                {
                    await _localStorage.SetItemAsync("refreshToken", response.RefreshToken);
                }
                await _localStorage.SetItemAsync("uid", response.Id);
                await _localStorage.SetItemAsync("role", response.Role!.ToString());
                await _localStorage.SetItemAsync("shopId", response.ShopId!);

                // Start SignalR connection with the user's JWT token
                // Derive the API root URL (strip trailing "/api/" if present)
                var apiBase = new Client.Handlers.Constants(_navigation, _configuration).BaseAddress();
                var hubBase = apiBase.EndsWith("/api/", StringComparison.OrdinalIgnoreCase)
                    ? apiBase[..^5]   // Remove "/api/"
                    : apiBase.TrimEnd('/');
                await _hubService.StartConnectionAsync(hubBase, response.Token);
            }            
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<string> GetTokenAsync()
            => await _localStorage.GetItemAsync<string>("token");

        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            return keyValuePairs?.Select(kvp => new Claim(kvp.Key, kvp!.Value!.ToString()!))!;
        }
        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
        public void LogOutNotfiy()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
