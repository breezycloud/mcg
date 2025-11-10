using Shared.Enums;

namespace Shared.Extensions;

public static class ProductExtensions
{
    public static bool IsCng(this Product p) =>
        p == Product.CngAbuja || p == Product.CngLagos;

    public static string ToDisplay(this Product p) => p switch
    {
        Product.CngAbuja => "CNG-Abuja",
        Product.CngLagos => "CNG-Lagos",
        Product.PMS => "PMS",
        Product.ATK => "ATK",
        Product.LPG => "LPG",
        Product.AGO => "AGO",
        _ => p.ToString()
    };

    public static string ToDisplay(this ProductFilter pf) => pf switch
    {
        ProductFilter.All => "All",
        ProductFilter.CngAbuja => "CNG-Abuja",
        ProductFilter.CngLagos => "CNG-Lagos",
        ProductFilter.PMS => "PMS",
        ProductFilter.ATK => "ATK",
        ProductFilter.LPG => "LPG",
        ProductFilter.AGO => "AGO",
        _ => pf.ToString()
    };
}