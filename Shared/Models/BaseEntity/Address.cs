using System.ComponentModel.DataAnnotations;

namespace Shared.Models.BaseEntity;


public class Address
{
    [Required(ErrorMessage = "Location is required.")]
    [StringLength(50, ErrorMessage = "Location cannot exceed 100 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "State is required.")]
    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters.")]
    public string State { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? ContactAddress { get; set; }
}