using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Helpers;

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

    public override string ToString()
    {
        return $"{FirstName} {LastName}";
    }
}