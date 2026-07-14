namespace Shared.Models.Stations;

public class StationFleetMetricsDto
{
    // North-star KPIs
    public decimal ShortageRate { get; set; }  // Volume-weighted: total shortage / total dispatched, within the period

    // Supporting KPIs
    public int TotalStations { get; set; }
    public int StationsActiveInPeriod { get; set; }
    public int TripsInPeriod { get; set; }
    public decimal TotalQuantityDischarged { get; set; }
}

// One row per station — the operational "what needs my attention" table. TotalDispatched is
// the loaded quantity of every trip that discharged here at all (a multi-destination trip's
// full loaded amount can count toward more than one station — same approximation the existing
// single-station drill-down already makes for ShortageAmount); TotalDischarged is scoped to
// just this station's portion, which is exact.
public class StationPerformanceRowDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal TotalDispatched { get; set; }
    public decimal TotalDischarged { get; set; }
    public decimal ShortageRate { get; set; }
    // This station's shortage volume as a % of the total shortage volume across every
    // station in the period — "of all the shortage that happened, how much is on us" — not
    // the same thing as ShortageRate (that station's own shortage relative to its own
    // volume). Drives the "Top Stations" ranking; ShortageRate stays a per-station rate.
    public decimal ShortageSharePercent { get; set; }
    public bool NeedsAttention { get; set; }
}

public class StationMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TripCount { get; set; }
    public decimal ShortageRate { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
