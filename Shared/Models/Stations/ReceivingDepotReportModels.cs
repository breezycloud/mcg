namespace Shared.Models.Stations;

// Receiving-depot counterpart to LoadingDepotReportModels.cs / StationFleetReportModels.cs.
// A Receiving Depot isn't a discharge point — it's a temporary holding stop a truck passes
// through (Trip.LoadingInfo.DispatchType == Depot) before being re-invoiced onward to the
// actual DischargeStation. Built from Trip.ReceivingDepotId + Trip.ArrivalInfo's depot fields
// (ArrivedDepot, DepotArrivalDateTime, InvoiceIssued, InvoiceToStationDateTime) — there's no
// shortage concept here at all, since no product changes hands at this stop. The operational
// question is how long trucks sit before being re-invoiced (dwell time), and how many are
// sitting there right now.
public class ReceivingDepotFleetMetricsDto
{
    // North-star KPI
    public decimal AvgDwellHours { get; set; }

    // Supporting KPIs
    public int TotalReceivingDepots { get; set; }
    public int DepotsActiveInPeriod { get; set; }
    public int TripsInPeriod { get; set; }
    // Live, not period-scoped — trucks that have arrived but haven't been re-invoiced yet,
    // right now, regardless of which period is selected.
    public int CurrentlyStuckCount { get; set; }
}

// One row per receiving depot.
public class ReceivingDepotPerformanceRowDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal AvgDwellHours { get; set; }
    public int CurrentlyStuckCount { get; set; }
    public bool NeedsAttention { get; set; }
}

public class ReceivingDepotMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TripCount { get; set; }
    public decimal AvgDwellHours { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
