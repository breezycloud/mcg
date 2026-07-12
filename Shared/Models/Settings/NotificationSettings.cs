using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Settings;

// Singleton row — there is only ever one. AuditInterceptor already logs who changed it and when
// (Api/Interceptors/AuditInterceptor.cs has no exclusion for this type), so only UpdatedAt lives
// here for the settings page's own "last saved" display.
public class NotificationSettings
{
    [Key]
    public Guid Id { get; set; }

    public string? NrlCcuEmail { get; set; }

    // Semicolon-separated list of CC recipients for the NRL CCU shortage notification.
    public string? NrlCcuCcEmails { get; set; }

    // When true, CNG trips (CngAbuja/CngLagos) are excluded from shortage dashboards, reports,
    // drill-downs, and CSV exports — see Product.IsCng(). Does NOT affect the CCU notification
    // pipeline: both CNG and LPG are permanently excluded from that regardless of this flag (see
    // ShortageNotificationService and ViewTrip.razor's CanRecordCcuRecommendation) since neither
    // product's shortage figures are reliable enough to act on.
    public bool ExcludeCngFromShortage { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
