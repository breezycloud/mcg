using System.Net.Http.Json;
using Microsoft.JSInterop;
using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Interfaces.Trips;


public class TripService(IHttpClientFactory _httpClient, IJSRuntime js) : ITripService
{
    public Trip MapTripLoadingAsync(TripLoadingDto model, CancellationToken cancellationToken)
    {
        Guid id = Guid.NewGuid();
        return new Trip
        {
            Id = id,
            Date = model.LoadingDate!.Value,
            DriverId = model.DriverId,
            TruckId = model.TruckId,
            DispatchId = model.DispatchId,     
            LoadingDepotId = model.LoadingPointId,       
            Status = Enums.TripStatus.Active,
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
        model.SetInitialStatus();
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

    public async Task<Trip?> GetActiveTripForTruckAsync(Guid truckId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Trips/active/{truckId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Trip?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<List<Trip>> GetTripsByTruckAsync(Guid truckId, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Trips/truck-trips/{truckId}?year={year}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Trip>?>(cancellationToken) ?? [];
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<Trip[]> GetTripsByDateRangeAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Trips/trips-byrange", filter, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Trip[]?>(cancellationToken) ?? [];
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    

    public async Task<bool> UpdateAsync(Trip model, CancellationToken cancellationToken)
    {
        model.UpdateStatusWithLoadingInfo();
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

    public async ValueTask ExportToCsvAsync(ReportFilter request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Trips/report", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var fileName = $"{request.StartDate:MMMM-yyyy} Trips_{DateTime.Now:yyyyMMddHHmmss}.csv";
            // In a Blazor WebAssembly app, use JS interop to save the file
            await js.InvokeVoidAsync("downloadReport", fileName, Convert.ToBase64String(content));
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    public async Task<string?> GenerateDispatchIdAsync(Guid truckId, DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Trips/generate-dispatch?truckId={truckId}&date={date:yyyy-MM-dd}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<string?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}