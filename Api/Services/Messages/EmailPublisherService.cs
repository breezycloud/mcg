using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shared.Models.MessageBroker;
namespace Api.Services.Messages;
public class EmailPublisherService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "email_exchange";
    private const string QueueName = "email_queue";

    public EmailPublisherService(IOptions<MessageBrokerSetting> config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config.Value.HostName,
            UserName = config.Value.UserName,
            Password = config.Value.Password,
            Port = config.Value.Port
        };


        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange and queue
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
        var queueArgs = new Dictionary<string, object>
        {
            { "x-message-ttl", 86400000 },
            { "x-dead-letter-exchange", "email_dead_letter_exchange" },
            { "x-dead-letter-routing-key", "email.send" }
        };
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(QueueName, ExchangeName, "email");
    }

    public void QueueEmailAsync(EmailQueueMessage message)
    {       

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "email",
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}