using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Incidents;


public class IncidentType
{
    [Key] public Guid Id { get; set; }
    [Required(ErrorMessage = "Field is required")]
    public string? Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public virtual ICollection<Incident> Incidents { get; set; } = [];
}