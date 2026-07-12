namespace Shared.Models.Stations;

// Refuelling-side counterpart to the other three station reports. Built from RefuelInfo
// (its own direct StationId FK — the entry form already restricts the picker to
// RefuellingStation-typed stations, so unlike the other three report types this data doesn't
// need any Trip cross-referencing at all). No shortage/rate concept applies here; the
// operational questions are volume, event frequency, and cost.
public class RefuellingStationFleetMetricsDto
{
    // North-star KPI — cost is the one figure comparable across stations regardless of which
    // product/unit (MT/SCM/KG/LTR) a given station refuels in.
    public decimal TotalCost { get; set; }

    // Supporting KPIs
    public int TotalRefuellingStations { get; set; }
    public int StationsActiveInPeriod { get; set; }
    public int RefuelEventCount { get; set; }
    public int TrucksServiced { get; set; }
}

// One row per refuelling station. TotalQuantity/Unit assume a station refuels in a single
// consistent unit (true in practice — a CNG mother station logs SCM, a diesel point logs LTR,
// etc.) — Unit is taken from whichever record actually has one set.
public class RefuellingStationPerformanceRowDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int RefuelEventCount { get; set; }
    public int TrucksServiced { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    // This station's cost as a % of the total refuelling cost across every station in the
    // period — same "share of the total" convention as the Discharge/Loading Depot reports,
    // and the reason cost (not quantity) drives the ranking: it stays comparable even when
    // stations refuel in different units.
    public decimal CostSharePercent { get; set; }
}

public class RefuellingStationMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int RefuelEventCount { get; set; }
    public decimal TotalCost { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
