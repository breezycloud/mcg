using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Helpers;
using Shared.Models.Trips;

namespace Shared.Models.Drivers;


public class Driver
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [Required]
    [StringLength(11, ErrorMessage = "Phone must be exactly 11 digits")]
    public string? PhoneNo { get; set; }
    public string? LicenseNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    [Column(TypeName = "jsonb")]
    public List<UploadResult> Files { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual ICollection<Trip>? Trips { get; set; } = [];

    public override string ToString()
    {
        return $"{FirstName} {LastName}";
    }
}

public class DriverReportFilters
{
    public string LicenseStatus { get; set; } = "";
    public string ActivityStatus { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class DriverReportDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DriverInitials { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseExpiry { get; set; } = string.Empty;
    public string LicenseStatus { get; set; } = string.Empty;
    public string JoinDate { get; set; } = string.Empty;
    public int TotalTrips { get; set; }
    public int CompletedTrips { get; set; }
    public int ThisMonthTrips { get; set; }
    public double AverageRating { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string? CurrentTrip { get; set; }
}


    public class DriverCsvExportDto
    {
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "License Number")]
        public string LicenseNumber { get; set; } = string.Empty;

        [Display(Name = "License Expiry")]
        public string LicenseExpiry { get; set; } = string.Empty;

        [Display(Name = "License Status")]
        public string LicenseStatus { get; set; } = string.Empty;

        [Display(Name = "Current Status")]
        public string CurrentStatus { get; set; } = string.Empty;

        [Display(Name = "Total Trips")]
        public int TotalTrips { get; set; }

        [Display(Name = "Completed Trips")]
        public int CompletedTrips { get; set; }

        [Display(Name = "This Month Trips")]
        public int ThisMonthTrips { get; set; }

        [Display(Name = "Average Rating")]
        public string AverageRating { get; set; } = string.Empty;

        [Display(Name = "Current Trip")]
        public string CurrentTrip { get; set; } = string.Empty;

        [Display(Name = "Join Date")]
        public string JoinDate { get; set; } = string.Empty;
    }