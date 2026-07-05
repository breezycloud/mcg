using Shared.Enums;

namespace Shared.Models.Reports;

/// <summary>
/// A single task item stored inside a DailyReport's WorkTasks or TomorrowTasks JSONB column.
/// Must be a class (not record) with a default constructor so Npgsql's dynamic JSON
/// serializer can deserialize it at runtime.
/// </summary>
public class ReportTask
{
    /// <summary>The planned task text. Written during report creation/editing.</summary>
    public string? Text { get; set; }

    /// <summary>
    /// What was actually done / notes on this task.
    /// Replaces the old parallel ActualTasks list.
    /// </summary>
    public string? ActualNote { get; set; }

    /// <summary>
    /// Completion status — updatable by the assigned employee at any time,
    /// even after the report is submitted, via PATCH /api/daily-reports/{id}/tasks.
    /// </summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
}
