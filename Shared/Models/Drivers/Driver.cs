using System.ComponentModel.DataAnnotations;

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
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"{FirstName} {LastName}";
    }
}