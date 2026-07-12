namespace Shared.Models.MessageBroker;

public class MailBodyBase
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? SupportEmail { get; set; } = "mcg@gmail.com";
    public string? ResetUrl { get; set; }
    // Drives the "this is a test email" banner in every template — true for anything that
    // isn't the real production environment (Staging, Development, etc).
    public bool IsTestEnvironment { get; set; }
}


public class AccountDetailBody : MailBodyBase
{
    public string? Password { get; set; }
    public string? PortalUrl { get; set; }
}

public class ShortageNotificationBody : MailBodyBase
{
    public string? TruckNo { get; set; }
    public string? TruckPlate { get; set; }
    public string? WaybillNo { get; set; }
    public string? DriverName { get; set; }
    public string? StationName { get; set; }
    public string? LoadingDate { get; set; }
    public decimal LoadedQuantity { get; set; }
    public decimal DischargedQuantity { get; set; }
    public decimal ShortageAmount { get; set; }
    public string? Unit { get; set; }
    // One row per compartment — the calibration-chart-style table (loading vs. arrival
    // ullage/liquid-height/overall) that CCU actually reviews.
    public List<ShortageCompartmentRow> Compartments { get; set; } = [];
}

public class ShortageCompartmentRow
{
    public string Compartment { get; set; } = string.Empty;
    public decimal QuantityLoaded { get; set; }
    public decimal? LoadingUllage { get; set; }
    public decimal? LoadingLiquidHeight { get; set; }
    public decimal? LoadingOverall { get; set; }
    public decimal? ArrivalUllage { get; set; }
    public decimal? ArrivalLiquidHeight { get; set; }
    public decimal? ArrivalOverall { get; set; }
}