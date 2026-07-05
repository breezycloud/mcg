namespace Shared.Helpers;

public class DailyReportFilter
{
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly? EndDate { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Status { get; set; }
}
