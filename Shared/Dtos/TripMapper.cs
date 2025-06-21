using Shared.Models.Trips;

namespace Shared.Dtos;


public static class TripMapper
{
    public static TripExportDto ToExportDto(Trip trip)
    {
        var dischargeSummary = trip.Discharges != null && trip.Discharges.Any()
            ? string.Join(" | ", trip.Discharges.Select(d =>
                $"{d.Location} ({d.QuantityDischarged:N0} {d.Trip?.Origin?.Unit}) - {d.DischargeStartTime:dd/MM/yyyy}"))
            : "None";

        return new TripExportDto
        {
            Date = trip.Date,
            DispatchId = trip.DispatchId,
            TruckPlate = trip.Truck?.LicensePlate ?? "N/A",
            Product = trip.Truck?.Product.ToString() ?? "N/A",
            Status = trip.Status.ToString(),
            LoadingPoint = trip.Origin?.Station?.Name ?? "N/A",
            WaybillNo = trip.WaybillNo,
            DispatchQuantity = trip.Origin?.Quantity ?? 0,
            DriverName = trip.Driver?.ToString() ?? "N/A",
            Dest = trip.Dest,
            ElockStatus = trip.ElockStatus.ToString(),
            ArrivedAtATV = trip.ArrivedAtATV ? "Yes" : "No",
            AtvArrivalDate = trip.ATVArrivalDate.HasValue ? trip.ATVArrivalDate.Value.ToString("dd/MM/yyyy") : "N/A",
            ArrivedAtStation = trip.Destination?.ArrivedAtStation == true ? "Yes" : "No",
            StationArrivalDate = trip.Destination?.StationArrivalDate.HasValue == true
                ? trip.Destination.StationArrivalDate.Value.ToString("dd/MM/yyyy")
                : "N/A",
            Discharged = trip.Destination?.IsDischarged == true ? "Yes" : "No",
            DischargeLocation = trip.Destination?.Station?.Name ?? "N/A",
            DischargedDate = trip.Destination?.DischargeDate.HasValue == true
                ? trip.Destination.DischargeDate.Value.ToString("dd/MM/yyyy")
                : "N/A",
            DischargedQuantity = trip.Destination?.DischargedQuantity ?? 0,
            DischargedUnit = trip.Origin?.Unit.ToString() ?? "N/A",
            HasShortage = trip.Destination?.HasShortage == true ? "Yes" : "No",
            ShortageAmount = trip.Destination?.ShortageAmount ?? 0,
            ReturnDate = trip.ReturnDate.HasValue
                ? trip.ReturnDate.Value.ToString("dd/MM/yyyy")
                : "N/A",

            DischargeSummary = dischargeSummary,
            DurationDays = trip.CalculateTripDuration(trip.Date, trip.ReturnDate),
            Notes = trip.Notes ?? "N/A"
        };
    }
    
    public static List<TripExportDto> ToExportDto(List<Trip> trips)
    {
        return trips.Select(ToExportDto).ToList();        
    }
}