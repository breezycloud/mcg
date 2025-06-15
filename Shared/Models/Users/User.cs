using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Shared.Models.Users;


public class User
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
    public string? PhoneNo { get; set; }
    [Required]
    public string? HashedPassword { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool? IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
}