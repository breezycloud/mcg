namespace Shared.Helpers;


public class VersionManifest
{
    public string AppVersion { get; set; } = string.Empty;
    public bool ForceUpdate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? ReleaseNotes { get; set; }
}