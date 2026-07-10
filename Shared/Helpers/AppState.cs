using System.Collections.Concurrent;
using Shared.Models.Drivers;
using Shared.Models.Trips;
using Shared.Enums;

namespace Shared.Helpers;


public partial class AppState
{
    public bool IsProcessing { get; set; } = false;
    public bool IsExporting { get; set; } = false;
    public bool IsBusy { get; set; }
    public bool ShowUpdateDialog { get; set; } = false;
    public Trip? Trip { get; set; }

    public string GetUnitForProduct(string? ProductFilter)
    {
        return ProductFilter switch
        {
            "CNG" => "SCM",
            "LPG" => "KG",
            _ => "LTR"
        };
    }

    public event EventHandler? FilterChanged;

    private bool _filterChanged = false;
    public bool HasChanged {
        get => _filterChanged;
        set {
            if (_filterChanged != value)
            {
                _filterChanged = value;
                FilterChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void OnFilterChanged()
    {
        HasChanged = true; // triggers the setter
    }

    public event EventHandler? RefuelProcessed;

    private bool _hasProcessed = false;
    public bool HasProcessed {
        get => _hasProcessed;
        set {
            if (_hasProcessed != value)
            {
                _hasProcessed = value;
                RefuelProcessed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void OnRefuelProcessed()
    {
        HasProcessed = true; // triggers the setter
    }

    public void Clear()
    {
        HasProcessed = false;
    }

    public CancellationToken GetCancellationToken() =>
        new CancellationTokenSource().Token;


    public TableState<Trip>? TripStore { get; set; } = new();
    public TableState<Driver>? DriverStore { get; set; } = new();
    // Managed products for DriverSupervisor (populated after auth)
    public List<Product> ManagedProducts { get; private set; } = new();

    public bool IsDriverSupervisor { get; private set; }

    public void SetDriverSupervisorContext(bool isDriverSupervisor, IEnumerable<Product> products)
    {
        IsDriverSupervisor = isDriverSupervisor;
        ManagedProducts = isDriverSupervisor ? products.Distinct().ToList() : new List<Product>();
    }

    // ─── Daily Report badge ───────────────────────────────────────────────────

    /// <summary>
    /// Count of submitted reports with no manager comment.
    /// Loaded by NavMenu on init. Decremented by SaveReview. Incremented by SignalR submission notification.
    /// </summary>
    private int _dailyReportBadgeCount;
    public int DailyReportBadgeCount
    {
        get => _dailyReportBadgeCount;
        set
        {
            var clamped = Math.Max(0, value);
            if (_dailyReportBadgeCount != clamped)
            {
                _dailyReportBadgeCount = clamped;
                DailyReportBadgeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? DailyReportBadgeChanged;

    // ─── Live dashboard refresh ────────────────────────────────────────────────

    /// <summary>
    /// Raised when a Trip or Discharge changed server-side (pushed via DashboardHub) — the
    /// payload is the entity type ("Trip"/"Discharge") so subscribers can ignore events that
    /// don't affect what they render. Debounced per entity type so a burst of rapid server
    /// broadcasts collapses into a single refresh per listener instead of one per event.
    /// </summary>
    public event EventHandler<string>? DashboardDataChanged;

    private readonly Dictionary<string, CancellationTokenSource> _dashboardDebounceTokens = new();
    private static readonly TimeSpan DashboardDebounceDelay = TimeSpan.FromSeconds(2);

    public void OnDashboardDataChanged(string entityType)
    {
        if (_dashboardDebounceTokens.TryGetValue(entityType, out var existingCts))
            existingCts.Cancel();

        var cts = new CancellationTokenSource();
        _dashboardDebounceTokens[entityType] = cts;

        _ = DebounceAndRaiseDashboardChangedAsync(entityType, cts.Token);
    }

    private async Task DebounceAndRaiseDashboardChangedAsync(string entityType, CancellationToken token)
    {
        try
        {
            await Task.Delay(DashboardDebounceDelay, token);
            DashboardDataChanged?.Invoke(this, entityType);
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer event for the same entity type before the delay elapsed.
        }
    }
}