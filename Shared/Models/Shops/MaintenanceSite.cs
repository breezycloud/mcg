using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.BaseEntity;

namespace Shared.Models.Shops;


public class MaintenanceSite
{
    [Key]
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? State { get; set; }
    [Column(TypeName = "jsonb")]
    public Address? Address { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}