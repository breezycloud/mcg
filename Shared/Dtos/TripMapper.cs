using Shared.Models.Trips;

namespace Shared.Dtos;


public static class TripMapper
{
    public static TripExportDto ToExportDto(Trip trip)
    {
        var dischargeSummary = trip.Discharges != null && trip.Discharges.Any()
            ? string.Join(" | ", trip.Discharges.Select(d =>
                $"{d.TruckArrival:g} {d.Station?.Name} ({d.QuantityDischarged:N0} {d.Trip?.GetUnit()}) - {d.DischargeStartTime:dd/MM/yyyy}"))
            : "None";

        var dischargeLocation = trip.Discharges != null && trip.Discharges.Any()
            ? string.Join(" | ", trip.Discharges.Select(d =>
                d.Station?.Name))
            : "None";
        var dischargeDate = trip.Discharges != null && trip.Discharges.Any()
            ? string.Join(" | ", trip.Discharges.Select(d =>
                $"{d.DischargeStartTime:g}"))
            : "None";
        return new TripExportDto
        {
            Date = trip.Date,
            DispatchId = trip.DispatchId,
            TruckPlate = trip.Truck?.LicensePlate ?? "N/A",
            Product = trip.Truck?.Product.ToString() ?? "N/A",
            Status = trip.Status.ToString(),
            LoadingPoint = trip.LoadingInfo.Destination ?? "N/A",
            WaybillNo = trip.LoadingInfo.WaybillNo,
            DispatchQuantity = trip.LoadingInfo?.Quantity ?? 0,
            DriverName = trip.Driver?.ToString() ?? "N/A",
            Dest = trip.LoadingInfo?.Destination,
            ElockStatus = trip.LoadingInfo?.ElockStatus.ToString(),
            ArrivedAtATV = trip.ArrivalInfo.ArrivedDepot ? "Yes" : "No",
            AtvArrivalDate = trip.ArrivalInfo.DepotArrivalDateTime.HasValue ? trip.ArrivalInfo.DepotArrivalDateTime.Value.ToString("dd/MM/yyyy") : "N/A",
            InvoiceDate = trip.ArrivalInfo.InvoiceToStationDateTime.HasValue ? trip.ArrivalInfo.InvoiceToStationDateTime.Value.ToString("dd/MM/yyyy") : "N/A",
            ArrivedAtStation = trip.ArrivalInfo.ArrivedAtStation == true ? "Yes" : "No",
            StationArrivalDate = trip.ArrivalInfo.StationArrivalDateTime.HasValue == true
                ? trip.ArrivalInfo.StationArrivalDateTime.Value.ToString("dd/MM/yyyy")
                : "N/A",
            Discharged = trip.Discharges!.Any() ? "Yes" : "No",
            DischargeLocation = dischargeLocation,
            DischargedDate = dischargeDate,
            DischargedQuantity = trip.Discharges!.Sum(x => x.QuantityDischarged),
            DischargedUnit = trip.GetUnit() ?? "N/A",
            HasShortage = trip.CalculateShortageOverage(trip.LoadingInfo?.Quantity - trip.Discharges!.Sum(x => x.QuantityDischarged)),
            ShortageAmount = trip.CalculateShortageOverageAmount(trip.LoadingInfo?.Quantity, trip.Discharges!.Sum(x => x.QuantityDischarged)) ?? 0,
            ReturnDate = trip.CloseInfo.ReturnDateTime.HasValue
                ? trip.CloseInfo.ReturnDateTime?.ToString("dd/MM/yyyy")
                : "N/A",

            DischargeSummary = dischargeSummary,
            DurationDays = trip.CalculateTripDuration(trip.Date, trip.CloseInfo.ReturnDateTime.HasValue ? DateOnly.FromDateTime(trip.CloseInfo.ReturnDateTime.Value) : null),
            Notes = trip.CloseInfo.TripRemark ?? "N/A"
        };
    }
    
    public static List<TripExportDto> ToExportDto(List<Trip> trips)
    {
        return trips.Select(ToExportDto).ToList();        
    }
}