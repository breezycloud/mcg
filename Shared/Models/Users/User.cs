using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shared.Enums;
using Shared.Helpers;

namespace Shared.Models.Users;

public class User
{
    [Key]
    public Guid Id { get; set; }
    public Guid? MaintenanceSiteId { get; set; }
    public Guid? SupervisorId { get; set; }
    public User? Supervisor { get; set; }
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
    public string? PhoneNo { get; set; }
    // Never legitimately read or written by a client — password changes go through
    // Auth/change-password, which hashes server-side and never round-trips the result to the
    // browser. [JsonIgnore] keeps it off the wire entirely, in both directions.
    [JsonIgnore]
    public string? HashedPassword { get; set; }
    [JsonIgnore]
    public string? PasswordResetToken { get; set; }
    [JsonIgnore]
    public DateTimeOffset? PasswordResetTokenExpiry { get; set; }
    public bool MustChangePassword { get; set; }
    public UserRole Role { get; set; } = UserRole.Admin;
    // Single profile photo, not a general file list — reuses UploadResult (same upload pipeline
    // as every other file in the app) purely for its PreviewUrl/ServerFileName shape.
    [Column(TypeName = "jsonb")]
    public UploadResult? Avatar { get; set; }
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


public class BrevoSettings
{
    public required string? Key { get; set; }
}