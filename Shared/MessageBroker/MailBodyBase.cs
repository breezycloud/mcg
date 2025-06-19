namespace Shared.Models.MessageBroker;

public class MailBodyBase
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? SupportEmail { get; set; } = "mcg@gmail.com";
}


public class AccountDetailBody : MailBodyBase
{
    public string? Password { get; set; }
    public string? PortalUrl { get; set; }    
}