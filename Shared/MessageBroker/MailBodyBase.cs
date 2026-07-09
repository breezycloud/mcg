namespace Shared.Models.MessageBroker;

public class MailBodyBase
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? SupportEmail { get; set; } = "mcg@gmail.com";
    public string? ResetUrl { get; set; }
    // Drives the "this is a test email" banner in every template — true for anything that
    // isn't the real production environment (Staging, Development, etc).
    public bool IsTestEnvironment { get; set; }
}


public class AccountDetailBody : MailBodyBase
{
    public string? Password { get; set; }
    public string? PortalUrl { get; set; }    
}