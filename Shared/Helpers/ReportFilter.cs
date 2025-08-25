using Shared.Enums;

namespace Shared.Helpers;

public class ReportFilter
{
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public DateOnly? EndDate { get; set; }
    public Guid? Id { get; set; }
    public TripStatus? Status { get; set; }
}