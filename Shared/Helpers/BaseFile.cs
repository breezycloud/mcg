namespace Shared.Helpers;

public class BaseFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTime UploadTime { get; set; } = DateTime.UtcNow;
    public string? PreviewUrl { get; set; }

    // Tags a file's role within whichever entity's Files list it lives in (e.g. a truck's
    // calibration chart vs. its other documents). Null means "untagged/general" — every file
    // list in the app predates this field, so old records simply deserialize with no category.
    public string? Category { get; set; }
}

// Well-known Category values. Plain strings rather than an enum: BaseFile is reused across
// many unrelated file lists (Driver docs, Truck docs, Discharge files, generic trip files),
// so no single enum would fit every context, and jsonb-serialized old records shouldn't break
// if a context-specific value is added later without a shared enum update.
public static class FileCategories
{
    public const string CalibrationChart = "CalibrationChart";
    public const string Waybill = "Waybill";
}