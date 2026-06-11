namespace Shared.Helpers;

/// <summary>
/// Monthly summary counts returned by GET /api/daily-reports/summary.
/// Used to populate the header stat cards on the list page.
/// </summary>
public class DailyReportSummary
{
    public int Total { get; set; }
    public int Submitted { get; set; }
    public int Draft { get; set; }
    /// <summary>Reports assigned to the current user by a manager (not self-created).</summary>
    public int AssignedToMe { get; set; }
}
