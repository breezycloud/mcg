namespace Shared.Models.Stations;

// Loading-side counterpart to StationFleetReportModels.cs's Discharge Station report. Built
// from Trip.LoadingDepotId + Trip.LoadingInfo.Quantity, not Discharge — a loading depot never
// appears on a Discharge record. ShortageRate here is downstream: of what was loaded at this
// depot, how much came up short by the time it was delivered — attributing receiving-side
// shortage back to its origin.
public class LoadingDepotFleetMetricsDto
{
    // North-star KPI
    public decimal ShortageRate { get; set; }

    // Supporting KPIs
    public int TotalLoadingDepots { get; set; }
    public int DepotsActiveInPeriod { get; set; }
    public int TripsInPeriod { get; set; }
    public decimal TotalQuantityLoaded { get; set; }
}

// One row per loading depot.
public class LoadingDepotPerformanceRowDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal TotalQuantityLoaded { get; set; }
    public decimal ShortageRate { get; set; }
    // This depot's downstream-shortage volume as a % of the total downstream-shortage volume
    // across every loading depot in the period — same "share of the total problem" concept as
    // StationPerformanceRowDto.ShortageSharePercent, kept consistent between the two reports.
    public decimal ShortageSharePercent { get; set; }
    public bool NeedsAttention { get; set; }
}

public class LoadingDepotMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TripCount { get; set; }
    public decimal ShortageRate { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
