using Shared.Models.Reports;

namespace Shared.Dtos;

public static class DailyReportMapper
{
    /// <summary>
    /// Expands a single DailyReport into one row per WorkTask.
    /// TomorrowTasks are interleaved: if both lists have the same index, they share a row;
    /// otherwise the longer list gets extra rows with the shorter side empty.
    /// </summary>
    public static List<DailyReportExportDto> ToExportDto(DailyReport report)
    {
        var employeeName = report.Employee is not null
            ? $"{report.Employee.FirstName} {report.Employee.LastName}".Trim()
            : string.Empty;

        var title      = report.Employee?.Role.ToString() ?? string.Empty;
        var department = report.Department?.ToString() ?? string.Empty;

        var rows = new List<DailyReportExportDto>();
        int sn = 1;

        int workCount     = report.WorkTasks.Count;
        int tomorrowCount = report.TomorrowTasks.Count;
        int maxRows       = Math.Max(workCount, tomorrowCount);

        for (int i = 0; i < maxRows; i++)
        {
            ReportTask? work     = i < workCount     ? report.WorkTasks[i]     : null;
            ReportTask? tomorrow = i < tomorrowCount ? report.TomorrowTasks[i] : null;

            bool hasContent = !string.IsNullOrWhiteSpace(work?.Text)
                           || !string.IsNullOrWhiteSpace(work?.ActualNote)
                           || !string.IsNullOrWhiteSpace(tomorrow?.Text)
                           || !string.IsNullOrWhiteSpace(tomorrow?.ActualNote);

            if (!hasContent && i > 0) continue;

            rows.Add(new DailyReportExportDto
            {
                SerialNo           = sn++,
                Date               = report.ReportDate.ToString("dd/MM/yyyy"),
                ReportNo           = report.ReportNo,
                EmployeeName       = employeeName,
                Department         = department,
                Title              = title,
                PlannedTask        = work?.Text ?? string.Empty,
                ActualTask         = work?.ActualNote ?? string.Empty,
                TaskStatus         = work?.Status.ToString() ?? string.Empty,
                Remark             = i == 0 ? report.Remark : string.Empty,
                TomorrowPlan       = tomorrow?.Text ?? string.Empty,
                Recommendation     = tomorrow?.ActualNote ?? string.Empty,
                TomorrowTaskStatus = tomorrow?.Status.ToString() ?? string.Empty,
                Status             = report.Status.ToString(),
                ManagerComment     = i == 0 ? report.ManagerComment : string.Empty
            });
        }

        // Ensure at least one row per report
        if (rows.Count == 0)
        {
            rows.Add(new DailyReportExportDto
            {
                SerialNo       = 1,
                Date           = report.ReportDate.ToString("dd/MM/yyyy"),
                ReportNo       = report.ReportNo,
                EmployeeName   = employeeName,
                Department     = department,
                Title          = title,
                Remark         = report.Remark,
                Status         = report.Status.ToString(),
                ManagerComment = report.ManagerComment
            });
        }

        return rows;
    }

    public static List<DailyReportExportDto> ToExportDto(List<DailyReport> reports)
    {
        var result = new List<DailyReportExportDto>();
        foreach (var r in reports)
            result.AddRange(ToExportDto(r));
        return result;
    }
}
