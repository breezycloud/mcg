namespace Shared.Dtos;

// One parsed+resolved row from the driver-import CSV. Produced identically by
// both the preview (dry-run) and commit endpoints, so the review grid the
// user confirms is built from exactly the same resolution logic that commit
// will actually act on.
public class DriverImportRowDto
{
    public int RowNumber { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNo { get; set; }
    public string? LicenseNo { get; set; }
    public string? ExpiryDateRaw { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public string? MotorMateName { get; set; }
    public string? MotorMatePhoneNo { get; set; }

    public string? AssignedTruckPlate { get; set; }

    /// <summary>Existing driver matched by normalized phone number, if any.</summary>
    public Guid? MatchedDriverId { get; set; }

    /// <summary>Existing motor mate matched by normalized phone number, if any.</summary>
    public Guid? MatchedMotorMateId { get; set; }

    /// <summary>Existing truck matched by license plate, if the row named one.</summary>
    public Guid? MatchedTruckId { get; set; }

    /// <summary>"Create" or "Update" — derived from MatchedDriverId.</summary>
    public string Action => MatchedDriverId.HasValue ? "Update" : "Create";

    /// <summary>Blocking problems — this row will not be imported.</summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>Non-blocking issues — row still imports, but flagged for follow-up.</summary>
    public List<string> Warnings { get; set; } = [];

    public bool HasErrors => Errors.Count > 0;
}

public class DriverImportPreviewResponse
{
    public List<DriverImportRowDto> Rows { get; set; } = [];
    public int NewCount => Rows.Count(r => !r.HasErrors && r.Action == "Create");
    public int UpdateCount => Rows.Count(r => !r.HasErrors && r.Action == "Update");
    public int ErrorCount => Rows.Count(r => r.HasErrors);
    public int WarningCount => Rows.Count(r => !r.HasErrors && r.Warnings.Count > 0);
}

public class DriverImportCommitResponse
{
    public List<DriverImportRowDto> Rows { get; set; } = [];
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int MotorMatesCreated { get; set; }
    public int MotorMateHistoryEntries { get; set; }
    public int TrucksAssigned { get; set; }
}
