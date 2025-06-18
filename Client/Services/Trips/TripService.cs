using System.Net.Http.Json;
using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Trips;


public class TripService(IHttpClientFactory _httpClient) : ITripService
{
    public Trip MapTripLoadingAsync(TripLoadingDto model, CancellationToken cancellationToken)
    {
        Guid id = Guid.NewGuid();
        return new Trip
        {
            Id = id,
            Date = DateOnly.FromDateTime(model.LoadingDate!.Value),
            DriverId = model.DriverId,
            TruckId = model.TruckId,
            DispatchId = model.DispatchId,
            WaybillNo = model.WaybillNumber,
            Status = Enums.TripStatus.Active,
            Dest = model.Destination,
            Origin = new Origin
            {
                TripId = id,
                StationId = model.LoadingPointId,
                Quantity = model.DispatchQuantity,
                Unit = model.DispatchUnit
            },
            Destination = new Destination
            {
                TripId = id
            }
        };
    }
    // public Trip MapTripDischargeAsync(TripDischargingDto model, CancellationToken cancellationToken)
    // {
    //     Guid id = Guid.NewGuid();
    //     return new Trip
    //     {
    //         Id = id,
    //         Date = DateOnly.FromDateTime(model.LoadingDate!.Value),
    //         DriverId = model.DriverId,
    //         TruckId = model.TruckId,
    //         WaybillNo = model.WaybillNumber,
    //         Status = Enums.TripStatus.Active,
    //         Origin = new Destination
    //         {
    //             TripId = id,
    //             StationId = model.LoadingPointId,
    //             Quantity = model.DispatchQuantity,
    //             Unit = model.DispatchUnit
    //         }
    //     };
    // }
    public async Task<bool> AddAsync(Trip model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("Trips", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(Trip model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"Trips/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"Trips/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Trips/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Trip?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    
    public async Task<GridDataResponse<Trip>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Trips/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<Trip>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}