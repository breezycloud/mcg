
using Microsoft.AspNetCore.Components.Forms;
using Shared.Helpers;

namespace Client.Handlers;
public class FileUploadModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = "Upload file";
    public string InputId { get; set; } = $"file-upload-{Guid.NewGuid()}";

    // Styling
    public string Size { get; set; } = "default"; // "small", "default", "large"
    public string AdditionalClasses { get; set; } = "";

    // Configuration
    public bool Multiple { get; set; } = false;
    public string Accept { get; set; } = "*/*"; // e.g. "image/*,.pdf"
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

    // State
    public List<UploadedFile> Files { get; private set; } = new();
    public string ErrorMessage { get; set; } = "";
    public bool IsUploading { get; set; } = false;

}

public class UploadedFile : BaseFile
{
    public string? Error { get; set; }
    public IBrowserFile? BrowserFile { get; set; }

    // Helper property
    public string FormattedSize => Size switch
    {
        < 1024 => $"{Size} bytes",
        < 1024 * 1024 => $"{Size / 1024:N1} KB",
        _ => $"{Size / (1024 * 1024):N1} MB"
    };
}

