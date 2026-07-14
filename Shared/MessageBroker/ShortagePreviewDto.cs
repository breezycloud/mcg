namespace Shared.Models.MessageBroker;

// What the CCU-notification "Preview & Send" button on ViewTrip.razor works from — either the
// four requirements aren't all met yet (Ready = false, MissingRequirements lists what's left) or
// they are and Html carries the exact rendered email body that Send would actually mail out.
public class ShortagePreviewDto
{
    public bool Ready { get; set; }
    public bool AlreadySent { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public List<string> MissingRequirements { get; set; } = [];
    public string? Subject { get; set; }
    public string? To { get; set; }
    public string? Cc { get; set; }
    public string? Html { get; set; }
}
