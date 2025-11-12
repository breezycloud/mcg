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
}