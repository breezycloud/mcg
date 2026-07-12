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

    public DateTimeOffset? UpdatedAt { get; set; }
}
