using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Api.Context;
using Shared.Dtos;
using Shared.Enums;
using Shared.Helpers;
using Shared.Hubs;
using Shared.Models.Reports;
using Shared.Models.Users;

namespace Api.Controllers;

[Route("api/daily-reports")]
[ApiController]
[Authorize]
public class DailyReportsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<AppHub> _hubContext;

    public DailyReportsController(AppDbContext context, IHubContext<AppHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // ─── READ: Paginated list ──────────────────────────────────────────────────

    [HttpPost("paged")]
    public async Task<ActionResult<GridDataResponse<DailyReport>>> GetPagedAsync(
        [FromBody] GridDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var query = _context.DailyReports
            .AsNoTracking()
            .AsQueryable();

        // Visibility scoping:
        //   Admin/Master  → all reports
        //   Everyone else → their own reports, plus reports of users who have them as Supervisor
        //                    (role-agnostic — determined by User.SupervisorId, not the caller's role)
        if (!isAdminOrMaster)
            query = query.Where(x => x.EmployeeId == currentUserId || x.Employee!.SupervisorId == currentUserId);

        // Date filter (month + year)
        if (request.Date is not null)
            query = query.Where(x => x.ReportDate.Month == request.Date.Value.Month
                                  && x.ReportDate.Year == request.Date.Value.Year);

        // Status filter
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DailyReportStatus>(request.Status, out var status))
            query = query.Where(x => x.Status == status);

        // Search: single JOIN via navigation property — no separate Users query needed
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var pattern = $"%{request.SearchTerm}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Employee!.FirstName!, pattern) ||
                EF.Functions.ILike(x.Employee.LastName!, pattern)  ||
                (x.ReportNo != null && EF.Functions.ILike(x.ReportNo, pattern)));
        }

        var response = new GridDataResponse<DailyReport>
        {
            Total = await query.CountAsync(cancellationToken)
        };

        // Projection: skip the 4 JSONB task-list columns in the list query — load only display fields
        // Include Employee.Role so the client can display Title without a second request
        var projected = await query
            .OrderByDescending(x => x.ReportDate)
            .Skip(request.Paging)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,
                x.ReportDate,
                x.ReportNo,
                x.Department,
                x.Status,
                x.CreatedAt,
                x.UpdatedAt,
                x.EmployeeId,
                x.AssignedById,
                Employee = new { x.Employee!.FirstName, x.Employee.LastName, x.Employee.Role }
            })
            .ToListAsync(cancellationToken);

        response.Data = projected.Select(x => new DailyReport
        {
            Id = x.Id,
            ReportDate = x.ReportDate,
            ReportNo = x.ReportNo,
            Department = x.Department,
            Status = x.Status,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            EmployeeId = x.EmployeeId,
            AssignedById = x.AssignedById,
            Employee = new User { FirstName = x.Employee.FirstName, LastName = x.Employee.LastName, Role = x.Employee.Role }
        }).ToList();

        return Ok(response);
    }

    // ─── READ: Single report (full — includes JSONB task arrays) ───────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DailyReport>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var report = await _context.DailyReports
            .AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.ReviewedBy)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (report is null) return NotFound();

        // Non-admin/master users can only access their own reports, or reports of
        // users who have them set as Supervisor.
        if (!isAdminOrMaster && report.EmployeeId != currentUserId && report.Employee?.SupervisorId != currentUserId)
            return Forbid();

        return Ok(report);
    }

    // ─── READ: Monthly summary counts (for header stat cards) ─────────────────

    [HttpGet("summary")]
    public async Task<ActionResult<DailyReportSummary>> GetSummaryAsync(
        [FromQuery] DateOnly month,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var query = _context.DailyReports
            .AsNoTracking()
            .Where(x => x.ReportDate.Month == month.Month && x.ReportDate.Year == month.Year);

        if (!isAdminOrMaster)
            query = query.Where(x => x.EmployeeId == currentUserId || x.Employee!.SupervisorId == currentUserId);

        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total     = g.Count(),
                Submitted = g.Count(x => x.Status == DailyReportStatus.Submitted),
                Draft     = g.Count(x => x.Status == DailyReportStatus.Draft)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // AssignedToMe: reports where AssignedById != null AND EmployeeId == currentUser
        var assignedToMe = await _context.DailyReports
            .AsNoTracking()
            .CountAsync(x => x.EmployeeId == currentUserId
                          && x.AssignedById != null
                          && x.ReportDate.Month == month.Month
                          && x.ReportDate.Year == month.Year, cancellationToken);

        return Ok(new DailyReportSummary
        {
            Total        = counts?.Total ?? 0,
            Submitted    = counts?.Submitted ?? 0,
            Draft        = counts?.Draft ?? 0,
            AssignedToMe = assignedToMe
        });
    }

    // ─── READ: Get latest report before a date (for copy-yesterday feature) ────

    [HttpGet("latest-for-copy")]
    public async Task<ActionResult<DailyReport?>> GetLatestForCopyAsync(
        [FromQuery] Guid employeeId,
        [FromQuery] DateOnly beforeDate,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        // Only allow fetching for yourself, or Admin/Master for any user
        if (!isAdminOrMaster && employeeId != currentUserId)
            return Forbid();

        var latest = await _context.DailyReports
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.ReportDate < beforeDate)
            .OrderByDescending(x => x.ReportDate)
            .Select(x => new DailyReport
            {
                Id            = x.Id,
                ReportDate    = x.ReportDate,
                TomorrowTasks = x.TomorrowTasks
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null) return NoContent(); // 204 — no previous report found
        return Ok(latest);
    }

    // ─── READ: Generate next report number ────────────────────────────────────
    // Admin/Master can pass an explicit employeeId to generate a number for another user.
    // Everyone else always generates for themselves.

    [HttpGet("generate-no")]
    public async Task<ActionResult<string>> GenerateReportNo(
        [FromQuery] DateOnly date,
        [FromQuery] Guid? employeeId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        // Non-admin users can only generate for themselves
        var targetEmployeeId = isAdminOrMaster && employeeId.HasValue
            ? employeeId.Value
            : currentUserId;

        var count = await _context.DailyReports
            .Where(x => x.EmployeeId == targetEmployeeId
                     && x.ReportDate.Month == date.Month
                     && x.ReportDate.Year == date.Year)
            .CountAsync(cancellationToken);

        return Ok($"DR-{date.Year}-{date.Month:D2}-{count + 1:D3}");
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<DailyReport>> CreateAsync(
        [FromBody] DailyReport model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        model.Id = Guid.NewGuid();
        model.Status = DailyReportStatus.Draft;
        model.CreatedAt = DateTimeOffset.UtcNow;
        model.UpdatedAt = null;
        model.ManagerComment = null;
        model.ReviewedById = null;
        model.ReviewedAt = null;
        model.Employee = null;    // Detach navigation property before save
        model.AssignedBy = null;
        model.ReviewedBy = null;

        if (isAdminOrMaster && model.EmployeeId != Guid.Empty && model.EmployeeId != currentUserId)
        {
            // Admin/Master is assigning the report to another user — validate target exists
            var targetExists = await _context.Users.AnyAsync(u => u.Id == model.EmployeeId, cancellationToken);
            if (!targetExists)
                return BadRequest("The specified employee does not exist.");

            model.AssignedById = currentUserId;
        }
        else
        {
            // Non-admin: always create for themselves, prevent spoofing
            model.EmployeeId = currentUserId;
            model.AssignedById = null;
        }

        // ── Duplicate guard: one report per employee per day ──────────────────
        var duplicate = await _context.DailyReports
            .AsNoTracking()
            .Where(x => x.EmployeeId == model.EmployeeId && x.ReportDate == model.ReportDate)
            .Select(x => new { x.Id, x.ReportNo })
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicate is not null)
            return Conflict(new
            {
                message = $"A report for this date already exists (No. {duplicate.ReportNo ?? duplicate.Id.ToString()}).",
                existingId = duplicate.Id
            });

        // ── Always generate ReportNo server-side to prevent nulls from client failures ──
        var monthCount = await _context.DailyReports
            .CountAsync(x => x.EmployeeId == model.EmployeeId
                          && x.ReportDate.Month == model.ReportDate.Month
                          && x.ReportDate.Year  == model.ReportDate.Year,
                        cancellationToken);
        model.ReportNo = $"DR-{model.ReportDate.Year}-{model.ReportDate.Month:D2}-{monthCount + 1:D3}";

        _context.DailyReports.Add(model);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505")
        {
            // Last-resort race guard: two near-simultaneous submissions both passed the
            // duplicate check above before either committed — UX_DailyReports_Employee_Date is
            // what actually stops the second one. Same pattern as the Trip dispatch race guard.
            _context.ChangeTracker.Clear();
            return Conflict(new { message = "A report for this date already exists." });
        }

        // Notify the target employee via SignalR if this report was assigned by a manager
        if (model.AssignedById.HasValue)
        {
            await _hubContext.Clients
                .User(model.EmployeeId.ToString())
                .SendAsync("ReceiveReportAssigned",
                    new { ReportNo = model.ReportNo, ReportId = model.Id },
                    cancellationToken);
        }

        // Return the full persisted entity so the client gets the server-assigned Id.
        // Uses an explicit URI rather than CreatedAtAction(nameof(GetAsync), ...) —
        // the latter throws "No route matches the supplied values" while building
        // the Location header on every single call (confirmed via reproduction and
        // the Logs table), because this controller's route is a literal
        // "api/daily-reports" with no [controller] token, which action-name-based
        // link generation can't resolve back to. The DB save above already
        // succeeded by this point regardless — this bug only broke the response,
        // silently succeeding server-side while the client saw "operation failed".
        return Created($"api/daily-reports/{model.Id}", model);
    }

    // ─── UPDATE ───────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] DailyReport model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var existing = await _context.DailyReports.FindAsync([id], cancellationToken);
        if (existing is null) return NotFound();

        if (!isAdminOrMaster && existing.EmployeeId != currentUserId)
            return Forbid();

        if (existing.Status == DailyReportStatus.Submitted && !isAdminOrMaster)
            return BadRequest("Submitted reports cannot be edited.");

        existing.ReportDate   = model.ReportDate;
        existing.Department   = model.Department;
        existing.WorkTasks     = model.WorkTasks;
        existing.Remark        = model.Remark;
        existing.TomorrowTasks = model.TomorrowTasks;
        existing.UpdatedAt    = DateTimeOffset.UtcNow;

        // Capture the new employee ID BEFORE mutating existing.EmployeeId.
        // We need it after SaveChanges for the SignalR notification — if we read
        // existing.EmployeeId after the assignment below it would already equal model.EmployeeId.
        Guid? reassignedToEmployeeId = null;
        if (isAdminOrMaster && model.EmployeeId != Guid.Empty && model.EmployeeId != existing.EmployeeId)
        {
            var targetExists = await _context.Users.AnyAsync(u => u.Id == model.EmployeeId, cancellationToken);
            if (!targetExists)
                return BadRequest("The specified employee does not exist.");

            reassignedToEmployeeId = model.EmployeeId;
            existing.EmployeeId    = model.EmployeeId;
            existing.AssignedById  = currentUserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Notify the newly-assigned employee if a reassignment occurred
        if (reassignedToEmployeeId.HasValue)
        {
            await _hubContext.Clients
                .User(reassignedToEmployeeId.Value.ToString())
                .SendAsync("ReceiveReportAssigned",
                    new { ReportNo = existing.ReportNo, ReportId = existing.Id },
                    cancellationToken);
        }

        return NoContent();
    }

    // ─── SUBMIT: Draft → Submitted ────────────────────────────────────────────

    [HttpPut("{id:guid}/submit")]
    public async Task<IActionResult> SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var report = await _context.DailyReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();

        if (!isAdminOrMaster && report.EmployeeId != currentUserId)
            return Forbid();

        if (report.Status == DailyReportStatus.Submitted)
            return BadRequest("Report is already submitted.");

        report.Status = DailyReportStatus.Submitted;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify Admin/Master (unrestricted visibility) via the broadcast group, plus the
        // submitter's own Supervisor directly if one is set — matches the same visibility
        // rule as GetPendingReviewCountAsync so nobody's badge increments for a report they
        // can't actually see.
        var payload = new { ReportNo = report.ReportNo, ReportId = report.Id };
        await _hubContext.Clients
            .Group(AppHub.ManagersGroup)
            .SendAsync("ReceiveReportSubmitted", payload, cancellationToken);

        var supervisor = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == report.EmployeeId)
            .Select(u => new { u.SupervisorId, SupervisorRole = u.Supervisor!.Role })
            .FirstOrDefaultAsync(cancellationToken);

        // Skip if the supervisor is Admin/Master — they already got this via the group
        // broadcast above, and a second targeted send would double-increment their badge.
        if (supervisor?.SupervisorId is Guid supervisorId
            && supervisor.SupervisorRole is not (UserRole.Admin or UserRole.Master))
        {
            await _hubContext.Clients
                .User(supervisorId.ToString())
                .SendAsync("ReceiveReportSubmitted", payload, cancellationToken);
        }

        return NoContent();
    }

    // ─── REVIEW: Save manager feedback ────────────────────────────────────────

    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> ReviewAsync(
        Guid id,
        [FromBody] string comment,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var report = await _context.DailyReports
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (report is null) return NotFound();

        // Only Admin/Master, or the report owner's Supervisor, can review it.
        if (!isAdminOrMaster && report.Employee?.SupervisorId != currentUserId)
            return Forbid();

        report.ManagerComment = comment?.Trim();
        report.ReviewedById = currentUserId;
        report.ReviewedAt = DateTimeOffset.UtcNow;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Master")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.DailyReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();

        _context.DailyReports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ─── PATCH: Update task statuses (todo toggle) ─────────────────────────────
    // Separate from PUT update so status toggling works even on submitted reports.
    // Only the assigned employee or Admin/Master can call this.

    [HttpPatch("{id:guid}/tasks")]
    public async Task<IActionResult> UpdateTasksAsync(
        Guid id,
        [FromBody] DailyReport model,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var existing = await _context.DailyReports.FindAsync([id], cancellationToken);
        if (existing is null) return NotFound();

        if (!isAdminOrMaster && existing.EmployeeId != currentUserId)
            return Forbid();

        // Only update the task arrays — nothing else is allowed via this endpoint.
        // Validates that the incoming list sizes don't grow beyond what was already saved.
        if (model.WorkTasks.Count > existing.WorkTasks.Count)
            return BadRequest("Cannot add tasks via this endpoint. Use PUT to edit the full report.");
        if (model.TomorrowTasks.Count > existing.TomorrowTasks.Count)
            return BadRequest("Cannot add tasks via this endpoint. Use PUT to edit the full report.");

        existing.WorkTasks     = model.WorkTasks;
        existing.TomorrowTasks = model.TomorrowTasks;
        // Intentionally NOT updating UpdatedAt — task status toggling is operational,
        // not a report metadata change.

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ─── REVERT: Submitted → Draft ────────────────────────────────────────────

    [HttpPut("{id:guid}/revert")]
    [Authorize(Roles = "Admin,Master")]
    public async Task<IActionResult> RevertAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _context.DailyReports.FindAsync([id], cancellationToken);
        if (report is null) return NotFound();

        if (report.Status != DailyReportStatus.Submitted)
            return BadRequest("Only Submitted reports can be reverted to Draft.");

        report.Status = DailyReportStatus.Draft;
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // ─── READ: Count of submitted reports with no manager comment ─────────────
    // Used by the nav badge to show unreviewed report count to managers.

    [HttpGet("pending-review-count")]
    public async Task<ActionResult<int>> GetPendingReviewCountAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var query = _context.DailyReports
            .AsNoTracking()
            .Where(x => x.Status == DailyReportStatus.Submitted
                     && (x.ManagerComment == null || x.ManagerComment == string.Empty));

        // Everyone but Admin/Master only counts reports from users who have them as Supervisor.
        if (!isAdminOrMaster)
            query = query.Where(x => x.Employee!.SupervisorId == currentUserId);

        return Ok(await query.CountAsync(cancellationToken));
    }

    // ─── CSV EXPORT ───────────────────────────────────────────────────────────

    [HttpPost("report")]
    public async Task<IActionResult> ExportReport(
        [FromBody] DailyReportFilter request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var currentRole = User.FindFirstValue(ClaimTypes.Role);
        bool isAdminOrMaster = currentRole is "Admin" or "Master";

        var startDateTime = request.StartDate.ToDateTime(TimeOnly.MinValue);
        var endDate = request.EndDate ?? request.StartDate;
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

        var query = _context.DailyReports
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.ReportDate >= DateOnly.FromDateTime(startDateTime)
                     && x.ReportDate <= DateOnly.FromDateTime(endDateTime));

        // Everyone but Admin/Master is limited to their own reports plus their direct reports'.
        if (!isAdminOrMaster)
            query = query.Where(x => x.EmployeeId == currentUserId || x.Employee!.SupervisorId == currentUserId);

        if (request.EmployeeId.HasValue)
            query = query.Where(x => x.EmployeeId == request.EmployeeId.Value);

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DailyReportStatus>(request.Status, out var status))
            query = query.Where(x => x.Status == status);

        var reports = await query.OrderBy(x => x.ReportDate).ThenBy(x => x.Employee!.LastName).ToListAsync(cancellationToken);
        var rows = DailyReportMapper.ToExportDto(reports);

        var csv = new StringBuilder();
        csv.AppendLine("S/N,Date,Report No.,Name,Department,Title,Planned Work Task,Actual Job Done,Task Status,Remark,Tomorrow's Work Plan,Tomorrow Recommendation,Tomorrow Task Status,Status,Manager Feedback");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                EscapeCsv(row.SerialNo.ToString()),
                EscapeCsv(row.Date),
                EscapeCsv(row.ReportNo),
                EscapeCsv(row.EmployeeName),
                EscapeCsv(row.Department),
                EscapeCsv(row.Title),
                EscapeCsv(row.PlannedTask),
                EscapeCsv(row.ActualTask),
                EscapeCsv(row.TaskStatus),
                EscapeCsv(row.Remark),
                EscapeCsv(row.TomorrowPlan),
                EscapeCsv(row.Recommendation),
                EscapeCsv(row.TomorrowTaskStatus),
                EscapeCsv(row.Status),
                EscapeCsv(row.ManagerComment)
            }));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"Daily_Report_{request.StartDate:MMMM-yyyy}"
                     + (request.EndDate.HasValue ? $"_to_{request.EndDate:MMMM-yyyy}" : "")
                     + ".csv";

        return new FileStreamResult(new MemoryStream(bytes), "text/csv")
        {
            FileDownloadName = fileName
        };
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        value = value.Replace("\"", "\"\"");
        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r') || value.Contains('"'))
            value = $"\"{value}\"";
        return value;
    }
}
