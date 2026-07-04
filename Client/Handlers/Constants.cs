using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Client.Handlers;

// Standalone Blazor WASM has no per-environment appsettings file here: with no
// `blazor-environment` meta tag in index.html, and CI always publishing with
// `-c Release` for both staging and prod, the WASM host environment resolves to
// "Production" in every deployment — appsettings.Staging.json would never load.
// So environment is detected at runtime from the hostname instead, and the
// actual URLs live in the one appsettings.json file that's always loaded.
public class Constants(NavigationManager nav, IConfiguration configuration)
{
    private readonly NavigationManager _nav = nav;
    private readonly IConfiguration _configuration = configuration;

    public string Url { get; } = "AppUrl";
    public string BaseAddress()
    {
        var host = new Uri(_nav.BaseUri).Host;
        if (host.StartsWith("staging"))
        {
            return _configuration["ApiBaseUrls:Staging"] ?? "https://staging.atlanticlogistics-atv.com.ng/api/";
        }
        else if (host.Contains("atlanticlogistics-atv.com.ng"))
        {
            return _configuration["ApiBaseUrls:Production"] ?? "https://atlanticlogistics-atv.com.ng/api/";
        }
        else
        {
            return _configuration["ApiBaseUrls:Local"] ?? "https://localhost:7229/api/";
        }
    }
}