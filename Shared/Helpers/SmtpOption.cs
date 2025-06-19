namespace Shared.Helpers;

public class SmtpOption
{
    public const string Key = "Brevo";
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

}