namespace Shared.Models.MessageBroker;
public class MessageBrokerSetting
{
    public const string Key = "RabbitMQ";
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int Port { get; set; } = 5672;
}