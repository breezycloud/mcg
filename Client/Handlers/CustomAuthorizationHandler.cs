using Blazored.LocalStorage;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Shared.Models.Auth;

namespace Client.Handlers;

public class CustomAuthorizationHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    // Shared across every request this handler processes (it's registered Transient, but .NET's
    // HttpClientFactory reuses the underlying handler chain for a while, and even if it didn't,
    // a static lock is what actually matters here) — without it, five components all making a
    // request the moment the access token expires would each independently see a 401 and each
    // fire their own refresh call, racing to consume/rotate the same refresh token. Only the
    // first caller actually calls the server; everyone else awaits that same in-flight result.
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static Task<string?>? _inFlightRefresh;

    public CustomAuthorizationHandler(
        ILocalStorageService localStorageService,
        NavigationManager navigationManager,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _localStorageService = localStorageService;
        _navigationManager = navigationManager;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var jwtToken = await _localStorageService.GetItemAsync<string>("token");
        if (jwtToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

        var response = await base.SendAsync(request, cancellationToken);

        // /auth/* itself returning 401 (e.g. a bad login) must never trigger a refresh attempt —
        // that's not an expired-token case, and refreshing here would just be wrong/wasteful.
        if (response.StatusCode != HttpStatusCode.Unauthorized || IsAuthEndpoint(request))
        {
            return response;
        }

        response.Dispose();

        var newAccessToken = await RefreshAccessTokenAsync(cancellationToken);
        if (newAccessToken is null)
        {
            // No refresh token, or the refresh itself failed (expired/revoked/stolen-and-already-
            // rotated) — nothing left to try. The original 401 stands; GetAuthenticationStateAsync
            // will pick up on the missing/expired token on its next check and route to login.
            return await base.SendAsync(CloneRequest(request), cancellationToken);
        }

        var retryRequest = CloneRequest(request);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static bool IsAuthEndpoint(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/auth/", StringComparison.OrdinalIgnoreCase) == true;

    // HttpRequestMessage instances can only be sent once — a retry needs a fresh instance. The
    // Content reference itself is reused rather than re-copied: every caller in this codebase
    // builds requests via PostAsJsonAsync/GetAsync, whose content is fully buffered in memory
    // (JsonContent/StringContent), so it's safe to read a second time.
    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Content = original.Content,
            Version = original.Version
        };
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        foreach (var option in original.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }
        return clone;
    }

    private async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Lock-free fast path: if a refresh is already running, piggyback on it directly rather
        // than queuing behind the semaphore — by the time a queued waiter got through the lock
        // below, the in-flight task would already be nulled out by its own finally block (it's
        // cleared before the lock is released), so that check would never actually hit. Checking
        // here, before ever touching the lock, is what makes concurrent 401s from several
        // components actually share one refresh instead of each doing (and rotating) their own.
        var existing = _inFlightRefresh;
        if (existing is not null)
        {
            return await existing;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check: another caller may have started (and possibly even finished) a refresh
            // between our snapshot above and actually acquiring the lock.
            existing = _inFlightRefresh;
            if (existing is not null)
            {
                return await existing;
            }

            var refreshTask = DoRefreshAsync(cancellationToken);
            _inFlightRefresh = refreshTask;
            try
            {
                return await refreshTask;
            }
            finally
            {
                _inFlightRefresh = null;
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> DoRefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await _localStorageService.GetItemAsync<string>("refreshToken");
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        try
        {
            // A fresh, unnamed client — deliberately NOT the "AppUrl" named client, since that
            // would re-enter this same handler's pipeline (it's the one registered with
            // AddHttpMessageHandler<CustomAuthorizationHandler>).
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(new Constants(_navigationManager, _configuration).BaseAddress());

            using var response = await client.PostAsJsonAsync("auth/refresh", new RefreshTokenModel { RefreshToken = refreshToken }, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // The refresh token itself is dead (expired/revoked) — clear it so we don't keep
                // retrying with a token that will never work, and so a future login starts clean.
                await _localStorageService.RemoveItemAsync("token");
                await _localStorageService.RemoveItemAsync("refreshToken");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse?>(cancellationToken: cancellationToken);
            if (result?.Token is null)
            {
                return null;
            }

            await _localStorageService.SetItemAsync("token", result.Token);
            if (!string.IsNullOrEmpty(result.RefreshToken))
            {
                await _localStorageService.SetItemAsync("refreshToken", result.RefreshToken);
            }

            return result.Token;
        }
        catch (Exception)
        {
            // Network failure during refresh — leave the stored tokens alone (they may still be
            // valid; this could just be offline/a dropped connection) and let this one request
            // fail as an ordinary 401 rather than wiping a possibly-still-good session.
            return null;
        }
    }
}
