using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Shared.Enums;

namespace Api.Util
{
    public static class Converter
    {
        public static bool TryParseProduct(string? value, out Product product)
        {
            product = Product.PMS;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var v = value.Trim().Replace("_", "-").ToLowerInvariant();

            if (v is "cng" or "cng-abuja" or "cngabuja") { product = Product.CngAbuja; return true; }
            if (v is "cng-lagos" or "cnglagos") { product = Product.CngLagos; return true; }

            if (Enum.TryParse<Product>(value, true, out var parsed))
            {
                product = parsed;
                return true;
            }

            return v switch
            {
                "ago" => Assign(out product, Product.AGO),
                "atk" => Assign(out product, Product.ATK),
                "lpg" => Assign(out product, Product.LPG),
                "pms" => Assign(out product, Product.PMS),
                _ => false
            };
        }

        private static bool Assign(out Product product, Product value)
        {
            product = value;
            return true;
        }
    }
}
