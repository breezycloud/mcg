using Shared.Helpers;
using Shared.Models.Reports;

namespace Shared.Interfaces.Reports;

public interface IDailyReportService
{
    Task<GridDataResponse<DailyReport>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken);
    Task<DailyReport?> GetAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Creates a new report and returns the persisted entity (with server-assigned Id).</summary>
    Task<DailyReport?> AddAsync(DailyReport model, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(DailyReport model, CancellationToken cancellationToken);
    /// <summary>
    /// Updates only the WorkTasks and TomorrowTasks lists (status toggles).
    /// Works on both Draft and Submitted reports — task status is operational metadata.
    /// </summary>
    Task<bool> UpdateTasksAsync(Guid id, DailyReport model, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SubmitAsync(Guid id, CancellationToken cancellationToken);
    Task<string?> GenerateReportNoAsync(DateOnly date, Guid? employeeId, CancellationToken cancellationToken);
    ValueTask ExportToCsvAsync(DailyReportFilter filter, CancellationToken cancellationToken);
    /// <summary>Saves a manager comment on a submitted report.</summary>
    Task<bool> ReviewAsync(Guid id, string comment, CancellationToken cancellationToken);
    /// <summary>Reverts a submitted report back to Draft (Admin/Master only).</summary>
    Task<bool> RevertAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>Monthly summary counts for the list page header cards.</summary>
    Task<DailyReportSummary?> GetSummaryAsync(DateOnly month, CancellationToken cancellationToken);
    /// <summary>Count of submitted reports with no manager comment. Used for the nav badge.</summary>
    Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Returns the most recent submitted/draft report for the given employee
    /// so the form can copy yesterday's TomorrowPlans into today's PlannedTasks.
    /// </summary>
    Task<DailyReport?> GetLatestForCopyAsync(Guid employeeId, DateOnly beforeDate, CancellationToken cancellationToken);
}
