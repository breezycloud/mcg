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
            "PMS" => "LTR",
            "ATK" => "MT",
            "LPG" => "KG",
            "AGO" => "LTR",
            _ => ""
        };
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