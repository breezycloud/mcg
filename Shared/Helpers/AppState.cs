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

    public CancellationToken GetCancellationToken() =>
        new CancellationTokenSource().Token;

}