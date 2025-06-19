namespace Shared.Models.MessageBroker;

public class EmailQueueMessage
{
    public string? Template { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public object? TemplateModel { get; set; }
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