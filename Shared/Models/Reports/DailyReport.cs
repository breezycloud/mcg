using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Users;

namespace Shared.Models.Reports;

public class DailyReport
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateOnly ReportDate { get; set; }

    [MaxLength(20)]
    public string? ReportNo { get; set; }

    public Guid EmployeeId { get; set; }

    /// <summary>
    /// The user who created/assigned this report.
    /// Null when the employee created their own report.
    /// Populated when Admin/Master assigns the report to another user.
    /// </summary>
    public Guid? AssignedById { get; set; }

    /// <summary>Department the employee belongs to at the time of the report.</summary>
    public Department? Department { get; set; }

    // Title is intentionally NOT stored — it is derived at runtime from Employee.Role.

    /// <summary>
    /// Work tasks for today: planned text, actual note, and completion status.
    /// Replaces the old parallel PlannedTasks + ActualTasks string lists.
    /// Stored as JSONB — each element is a ReportTask object.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<ReportTask> WorkTasks { get; set; } = [];

    [MaxLength(2000)]
    public string? Remark { get; set; }

    /// <summary>
    /// Tomorrow's planned tasks and recommendations.
    /// Replaces the old parallel TomorrowPlans + Recommendations string lists.
    /// Stored as JSONB — each element is a ReportTask object.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<ReportTask> TomorrowTasks { get; set; } = [];

    public DailyReportStatus Status { get; set; } = DailyReportStatus.Draft;

    /// <summary>
    /// Feedback written by the reviewing superior (Admin/Master/Supervisor).
    /// Null until a manager adds feedback after the report is submitted.
    /// </summary>
    [MaxLength(2000)]
    public string? ManagerComment { get; set; }

    /// <summary>The manager who reviewed and commented on the report.</summary>
    public Guid? ReviewedById { get; set; }

    /// <summary>When the manager comment was last saved.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public virtual User? Employee { get; set; }

    [ForeignKey(nameof(AssignedById))]
    public virtual User? AssignedBy { get; set; }

    [ForeignKey(nameof(ReviewedById))]
    public virtual User? ReviewedBy { get; set; }
}
