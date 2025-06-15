using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.BaseEntity;

namespace Shared.Models.Stations;


public class Station
{
    [Key]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "jsonb")]
    public Address? Address { get; set; }
    public bool IsDepot { get; set; } = false;
    public StationType Type { get; set;  }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}