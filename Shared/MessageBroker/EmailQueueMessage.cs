namespace Shared.Models.MessageBroker;

public class EmailQueueMessage
{
    public string? Template { get; set; }
    public string? To { get; set; }
    // Semicolon-separated CC recipients — split out in EmailConsumerService, not FluentEmail's
    // own To(string)/CC(string) overloads, which take a single literal address each.
    public string? Cc { get; set; }
    public string? Subject { get; set; }
    public object? TemplateModel { get; set; }

    // Files to attach from disk (looked up by ServerFileName under FileStorage:UploadPath).
    // Separate from TemplateModel since attachments are a delivery concern, not something
    // the Razor template itself needs to reference.
    public List<EmailAttachmentRef>? Attachments { get; set; }
}

public class EmailAttachmentRef
{
    public string ServerFileName { get; set; } = string.Empty;
    public string DisplayFileName { get; set; } = string.Empty;
}

public class Message
{

}

public class MailBodyItem
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? OtpCode { get; set; }
    public string? Status { get; set; }
    public string? ExpireDate { get; set; }
    public int ExpirationMinutes { get; set; }
    public string? SupportEmail { get; set; } = "arewataxi@gmail.com";
}