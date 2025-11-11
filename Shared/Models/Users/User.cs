using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shared.Enums;

namespace Shared.Models.Users;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public Guid? MaintenanceSiteId { get; set; }
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
    public string? PhoneNo { get; set; }
    public string? HashedPassword { get; set; }
    public UserRole Role { get; set; } = UserRole.Admin;
    [Column(TypeName = "jsonb")]
    public List<Product> ManagedProducts { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public bool? IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"{FirstName} {LastName}";
    }
}

public class EmailRequest
{
    public string Email { get; set; }
    public string Name { get; set; }
    public int templateId { get; set; }
}