using Shared.Models.Trips;

namespace Shared.Helpers;


public class AppState
{
    public bool IsProcessing { get; set; } = false;
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
    public bool HasChanged
    {
        get => _filterChanged;
        set
        {
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
    public bool HasProcessed
    {
        get => _hasProcessed;
        set
        {
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

}