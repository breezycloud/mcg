namespace Shared.Helpers;

public class BaseFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public DateTime UploadTime { get; set; } = DateTime.UtcNow;
    public string? PreviewUrl { get; set; }
}