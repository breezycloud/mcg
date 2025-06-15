namespace Shared.Helpers;


public class VersionManifest
{
    public double AppVersion { get; set; }
    public bool ForceUpdate { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string? ReleaseNotes { get; set; }
}