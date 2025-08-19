using System.Net.Http.Json;
using Shared.Dtos;
using Shared.Helpers;
using Shared.Interfaces.Discharges;
using Shared.Models.Trips;

namespace Client.Services.Discharges;


public class DischargeService(IHttpClientFactory _httpClient) : IDischargeService
{
    // public Discharge MapDischargeLoadingAsync(DischargeLoadingDto model, CancellationToken cancellationToken)
    // {
    //     Guid id = Guid.NewGuid();
    //     return new Discharge
    //     {
    //         Id = id,
    //         Date = DateOnly.FromDateTime(model.LoadingDate!.Value),
    //         DriverId = model.DriverId,
    //         TruckId = model.TruckId,
    //         DispatchId = model.DispatchId,
    //         WaybillNo = model.WaybillNumber,
    //         Status = Enums.DischargeStatus.Active,
    //         Dest = model.Destination,
    //         Origin = new Origin
    //         {
    //             DischargeId = id,
    //             StationId = model.LoadingPointId,
    //             Quantity = model.DispatchQuantity,
    //             Unit = model.DispatchUnit
    //         },
    //         Destination = new Destination
    //         {
    //             DischargeId = id
    //         }
    //     };
    // }
    // public Discharge MapDischargeDischargeAsync(DischargeDischargingDto model, CancellationToken cancellationToken)
    // {
    //     Guid id = Guid.NewGuid();
    //     return new Discharge
    //     {
    //         Id = id,
    //         Date = DateOnly.FromDateTime(model.LoadingDate!.Value),
    //         DriverId = model.DriverId,
    //         TruckId = model.TruckId,
    //         WaybillNo = model.WaybillNumber,
    //         Status = Enums.DischargeStatus.Active,
    //         Origin = new Destination
    //         {
    //             DischargeId = id,
    //             StationId = model.LoadingPointId,
    //             Quantity = model.DispatchQuantity,
    //             Unit = model.DispatchUnit
    //         }
    //     };
    // }
    public async Task<bool> AddAsync(Discharge model, CancellationToken cancellationToken)
    {
        try
        {
            model.Station = null;
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("Discharges", model, cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<bool> UpdateAsync(Discharge model, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"Discharges/{model.Id}", model, cancellationToken);
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
            using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"Discharges/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return response.IsSuccessStatusCode;
        }
        catch (System.Exception)
        {

            throw;
        }
    }
    public async Task<Discharge?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Discharges/{id}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Discharge?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }

    
    public async Task<GridDataResponse<Discharge>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Discharges/paged", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GridDataResponse<Discharge>?>();
        }
        catch (System.Exception)
        {

            throw;
        }
    }
}