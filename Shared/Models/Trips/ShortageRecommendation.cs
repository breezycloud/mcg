using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Users;

namespace Shared.Models.Trips;

// A record of NRL CCU's reply to a shortage notification — the figure/remarks they came
// back with after reviewing the calibration chart and waybill. One-to-many against Trip
// (not one-to-one) so a correction or second reply from CCU doesn't overwrite the first
// record; the UI always surfaces the latest as "current" with older ones kept as history.
public class ShortageRecommendation
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TripId { get; set; }

    public decimal RecommendedShortageAmount { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    // The ticket/reference number CCU's reply email carries (e.g. "#8578 PMS...").
    [StringLength(100)]
    public string? CcuReferenceNumber { get; set; }

    public DateTimeOffset ReceivedDate { get; set; }

    public Guid? RecordedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(TripId))]
    public virtual Trip? Trip { get; set; }

    [ForeignKey(nameof(RecordedById))]
    public virtual User? RecordedBy { get; set; }
}
