using System.Net.Http.Json;
using Shared.Helpers;
using Shared.Interfaces.Locations;

namespace Client.Services.Locations;


public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;

    public LocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async ValueTask<List<StateInfo>?> GetStatesWithLocalGovts(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync("locations/state-lgas.json", cancellationToken);
            return await response.Content.ReadFromJsonAsync<List<StateInfo>>(cancellationToken);
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async ValueTask<List<string>?> States(CancellationToken cancellationToken)
    {
        List<string> states = [];
        try
        {
            using var response = await _httpClient.GetAsync("locations/state-lgas.json", cancellationToken);
            await foreach (var state in response.Content.ReadFromJsonAsAsyncEnumerable<StateInfo?>(cancellationToken))
            {
                if (state is not null)
                {
                    states.Add(state!.State!);
                }
            }
            return states;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async ValueTask<List<string>?> GetLocalGovtsByState(string stateName, CancellationToken cancellationToken)
    {
        List<string> states = [];
        try
        {
            using var response = await _httpClient.GetAsync("locations/state-lgas.json", cancellationToken);
            await foreach (var state in response.Content.ReadFromJsonAsAsyncEnumerable<StateInfo?>(cancellationToken))
            {
                if (state is null)
                    continue;

                if (!state.State!.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                    continue;

                states = state?.LocalGovt ?? [] ;
            }
            return states;
        }
        catch (System.Exception)
        {

            throw;
        }       
    }
}