using Shared.Enums;
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
            DispatchId = trip.DispatchId?.Trim(),
            TruckPlate = trip.Truck?.LicensePlate ?? "N/A",
            Product = trip.Truck?.Product.ToString() ?? "N/A",
            Status = GetTripStatus(trip),
            LoadingPoint = trip.LoadingDepot?.Name ?? "N/A",
            LoadingDate = trip.LoadingInfo.LoadingDate?.ToString("dd/MM/yyyy"),
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
            DurationDays = trip.CalculateTripDuration(trip.Date, trip.CloseInfo.ReturnDateTime),
            Notes = trip.CloseInfo.TripRemark ?? "N/A"
        };
    }

    private static string GetTripStatus(Trip? currentTrip)
    {
        if (currentTrip != null && currentTrip.Status == TripStatus.Dispatched && string.IsNullOrWhiteSpace(currentTrip.LoadingInfo?.WaybillNo))
            return "Dispatched";

        if (currentTrip != null && !string.IsNullOrWhiteSpace(currentTrip.LoadingInfo?.WaybillNo) && (currentTrip.ArrivalInfo is null && !currentTrip.ArrivalInfo.ArrivedAtStation || !currentTrip.ArrivalInfo.ArrivedDepot))
            return "Delivery Trip";

        if (currentTrip != null && currentTrip.LoadingInfo?.DispatchType == DispatchType.Depot && currentTrip.ArrivalInfo?.ArrivedDepot == true && currentTrip.ArrivalInfo?.InvoiceIssued ==false)
            return "Arrived at Depot";

        
        if (currentTrip != null && currentTrip.LoadingInfo?.DispatchType == DispatchType.Depot && currentTrip.ArrivalInfo?.InvoiceIssued == true && currentTrip.Discharges is null || !currentTrip.Discharges.Any())
            return "Invoiced to Station";

        if (currentTrip != null && !currentTrip.Discharges.Any() && (currentTrip.ArrivalInfo?.ArrivedAtStation == true || currentTrip.ArrivalInfo?.ArrivedDepot == true))
            return "Arrived at Discharge";

        if (currentTrip != null && currentTrip.Discharges.Any() && currentTrip.Discharges.Count(d => d.IsFinalDischarge) < 1)
            return "Discharging";

        if (currentTrip != null && currentTrip.Discharges.Any(d => d.IsFinalDischarge))
            return "Return Trip";

        // if (currentTrip.Status == TripStatus.Closed || currentTrip.Status == TripStatus.Completed)
        //     return ("Awaiting Loading");

        // if (!truck.IsActive)
        //     return ("Out of Service", "Disabled");

        // if (truck.ServiceRequests.Any(x => x.Status == RequestStatus.InProgress))
        //     return ("Under Repair", "Service Request");

        return "Available";
    }
    
    public static List<TripExportDto> ToExportDto(List<Trip> trips)
    {
        return trips.Select(ToExportDto).ToList();        
    }
}