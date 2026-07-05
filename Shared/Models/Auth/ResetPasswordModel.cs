using System;
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Auth;

public class ResetPasswordModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string? Token { get; set; }

    [Required]
    [StringLength(255, ErrorMessage = "Must be between 8 and 255 characters", MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }
}
