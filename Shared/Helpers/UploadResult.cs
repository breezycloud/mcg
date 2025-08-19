namespace Shared.Helpers;


public class UploadResult : BaseFile
{
    public UploadResult()
    {
        
    }
    public UploadResult(UploadResultDto file)
    {
        Id = Guid.NewGuid().ToString();
        ContentType = file.ContentType;
        FileName = file.FileName;
        Size = file.Size;
        UploadTime = file.UploadTime;
        PreviewUrl = file.PreviewUrl;
        ServerFileName = file.ServerFileName;
    }

    public UploadResult(BaseFile file)
    {
        Id = file.Id;
        ContentType = file.ContentType;
        FileName = file.FileName;
        Size = file.Size;
        UploadTime = file.UploadTime;
        PreviewUrl = file.PreviewUrl;
    }

    public string? ServerFileName { get; set; }

    // Helper property
    public string FormattedSize => Size switch
    {
        < 1024 => $"{Size} bytes",
        < 1024 * 1024 => $"{Size / 1024:N1} KB",
        _ => $"{Size / (1024 * 1024):N1} MB"
    };
}

public class UploadResultDto : BaseFile
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ServerFileName { get; set; }
}