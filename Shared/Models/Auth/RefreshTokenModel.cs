using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Auth;

public class RefreshTokenModel
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
