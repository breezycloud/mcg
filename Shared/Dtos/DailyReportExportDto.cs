using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public class DailyReportExportDto
{
    [Display(Name = "S/N")]
    public int SerialNo { get; set; }

    [Display(Name = "Date")]
    public string Date { get; set; } = "";

    [Display(Name = "Report No.")]
    public string? ReportNo { get; set; }

    [Display(Name = "Name")]
    public string? EmployeeName { get; set; }

    [Display(Name = "Department")]
    public string? Department { get; set; }

    [Display(Name = "Title")]
    public string? Title { get; set; }

    [Display(Name = "Planned Work Task")]
    public string? PlannedTask { get; set; }

    [Display(Name = "Actual Job Done")]
    public string? ActualTask { get; set; }

    [Display(Name = "Task Status")]
    public string? TaskStatus { get; set; }

    [Display(Name = "Remark")]
    public string? Remark { get; set; }

    [Display(Name = "Tomorrow's Work Plan")]
    public string? TomorrowPlan { get; set; }

    [Display(Name = "Tomorrow Recommendation")]
    public string? Recommendation { get; set; }

    [Display(Name = "Tomorrow Task Status")]
    public string? TomorrowTaskStatus { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }

    [Display(Name = "Manager Feedback")]
    public string? ManagerComment { get; set; }
}
