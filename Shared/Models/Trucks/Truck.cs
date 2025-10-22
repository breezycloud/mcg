using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Helpers;
using Shared.Models.Drivers;
using Shared.Models.Services;
using Shared.Models.Trips;

namespace Shared.Models.Trucks;


public class Truck
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(20)]
    public string TruckNo { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string LicensePlate { get; set; } = string.Empty;

    [StringLength(50)]
    [Required]
    public string? Manufacturer { get; set; } = "FAW";

    [StringLength(20)]
    public string? Color { get; set; } = "White";

    [Required]
    [StringLength(30)]
    public string? VIN { get; set; }

    [StringLength(30)]
    public string? EngineNo { get; set; } = "Nil";
    public Guid? DriverId { get; set; }
    public Product? Product { get; set; }
    public CalibrationType CalibrationType { get; set; }
    [Column(TypeName = "jsonb")]
    public List<CalibrationParam>? CalibrationParams { get; set; } = [];
    public DateOnly? ExpiryDate { get; set;  }
    [Column(TypeName = "jsonb")]
    public List<UploadResult> Files { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public virtual ICollection<Trip>? Trips { get; set; } = [];
    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }
    [NotMapped]
    public bool IsSelected { get; set; }

    [NotMapped]
    public string? OriginalTruckNo => TruckNo;
    [NotMapped]
    public string? OriginalLicensePlate => LicensePlate;
    [NotMapped]
    public string? OriginalVIN => VIN;
    [NotMapped]
    public string? OriginalEngineNo => EngineNo;
    public string GetProductColor(string? product)
    {
        return product switch
        {
            "CNG" => "bg-blue-500",
            "PMS" => "bg-yellow-500",
            "ATK" => "bg-red-500",
            "LPG" => "bg-green-500",
            "AGO" => "bg-purple-500",
            _ => "bg-gray-500"
        };
    }
}

public class TruckReportFilters
{
    public string ProductType { get; set; } = "";
    public string TruckStatus { get; set; } = "";
    public string ReportMonth { get; set; } = "";
    public string ReportYear { get; set; } = "";
}

public class TruckReportDto
{
    public Guid TruckId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public string TruckStatus { get; set; } = string.Empty;
    public string Location { get; set; } = "";
    public int TotalLoading { get; set; }
    public Dictionary<string, int> MonthlyLoading { get; set; } = new();
}

public class TruckCsvExportDto
{
    [Display(Name = "Truck Number")]
    public string TruckNumber { get; set; } = string.Empty;

    [Display(Name = "Product")]
    public string Product { get; set; } = string.Empty;

    [Display(Name = "Truck Status")]
    public string TruckStatus { get; set; } = string.Empty;

    [Display(Name = "Total Loading")]
    public int TotalLoading { get; set; }

    [Display(Name = "Monthly Data")]
    public Dictionary<string, int> MonthlyData { get; set; } = new();
}