using Microsoft.AspNetCore.Components;

namespace Client.Handlers;


public class Constants(NavigationManager nav)
{
    private readonly NavigationManager _nav = nav;

    public string Url { get; } = "AppUrl";
    public string BaseAddress()
    {
        var host = new Uri(_nav.BaseUri).Host;
        Console.WriteLine("Host: {0}", host);
        if (host.StartsWith("staging"))
        {
            return "https://staging.atlanticlogistics-atv.com.ng/api/";
        }
        else if (host.Contains("atlanticlogistics-atv.com.ng"))
        {
            return "https://atlanticlogistics-atv.com.ng/api/";
        }
        else
        {
            return "https://localhost:7229/api/";
        }
    }
}