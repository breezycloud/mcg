using System.Linq;
using System.Text;
using System.Text.Json;
using FluentEmail.Core;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RazorLight;
using Shared.Models.MessageBroker;

namespace Api.Services.Messages;

public class EmailConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IFluentEmail _fluentEmail;
    private readonly RazorLightEngine _razorEngine;
    private readonly ILogger<EmailConsumerService> _logger;
    private readonly string _uploadRootPath;
    private readonly IConfiguration _configuration;

    public EmailConsumerService(
        IOptions<MessageBrokerSetting> rabbitConfig,
        IFluentEmail fluentEmail,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<EmailConsumerService> logger)
    {
        _fluentEmail = fluentEmail;
        _configuration = configuration;
        _logger = logger;

        // Same rooting logic as Program.cs's static-file mount — UploadsController's own
        // _uploadPath field skips this and can't be trusted for resolving a physical path.
        var rawUploadPath = configuration["FileStorage:UploadPath"]!;
        _uploadRootPath = Path.IsPathRooted(rawUploadPath)
            ? rawUploadPath
            : Path.Combine(environment.ContentRootPath, rawUploadPath);

        var factory = new ConnectionFactory
        {
            HostName = rabbitConfig.Value.HostName,
            UserName = rabbitConfig.Value.UserName,
            Password = rabbitConfig.Value.Password,
            Port = rabbitConfig.Value.Port
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        var assembly = typeof(Program).Assembly;
        _razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(assembly, "Api.EmailTemplates")
            .UseMemoryCachingProvider()
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("email_queue", durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<EmailQueueMessage>(Encoding.UTF8.GetString(body));
                _logger.LogInformation("Processing mail for {0}", message!.To);
                await ProcessEmailMessageAsync(message);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email message");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(
            queue: "email_queue",
            autoAck: false,
            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessEmailMessageAsync(EmailQueueMessage message)
    {
        var templatePath = $"{message.Template}.cshtml";
        var json = JsonSerializer.Serialize(message.TemplateModel);
        _logger.LogInformation("Processing email template {0} with model {1}", templatePath, json);

        // Template name picks the model shape to deserialize into — every template beyond
        // the default needs a case here, since the .cshtml itself only binds dynamically.
        dynamic model = message.Template switch
        {
            "TripDischargeShortage" => JsonSerializer.Deserialize<ShortageNotificationBody>(json)!,
            _ => JsonSerializer.Deserialize<AccountDetailBody>(json)!
        };

        // Explicitly typed, not `var` — model is dynamic, so CompileRenderAsync's result (and
        // everything built from it below) would otherwise stay dynamic all the way through,
        // deferring overload resolution to the runtime binder instead of the compiler. That's
        // exactly what broke CC(IEnumerable<string>) below: the runtime binder doesn't see it
        // and silently rebinds to CC(string, string), then fails converting the list.
        string htmlContent = await _razorEngine.CompileRenderAsync(templatePath, model);

        IFluentEmail email = _fluentEmail
            .To(message.To)
            .Subject(message.Subject)
            .Body(htmlContent, true);

        // CCU shortage notifications go out under their own sender address rather than the
        // default MIS one — must stay a Brevo-verified sender or the relay will reject the send.
        if (message.Template == "TripDischargeShortage")
        {
            var ccuSenderEmail = _configuration["Brevo:CcuSenderEmail"] ?? "mcc@atlanticco-ltd.com";
            var ccuSenderName = _configuration["Brevo:SenderName"];
            email.SetFrom(ccuSenderEmail, ccuSenderName);
        }

        var ccAddresses = (message.Cc ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var cc in ccAddresses)
        {
            email.CC(cc);
        }

        foreach (var attachmentRef in message.Attachments ?? [])
        {
            var attachment = ResolveAttachment(attachmentRef);
            if (attachment != null)
            {
                email.Attach(attachment);
            }
        }

        var response = await email.SendAsync();
        if (!response.Successful)
        {
            throw new InvalidOperationException(
                $"Email send failed for {message.To}: {string.Join("; ", response.ErrorMessages)}");
        }
        _logger.LogInformation("Mail successfully sent to {0}", message!.To);
    }

    // Missing/unreadable attachments don't fail the whole send — a late or incomplete
    // notice still beats none, and an exception here would otherwise put this message into
    // the infinite BasicNack(requeue:true) loop below for a file that will never come back.
    private FluentEmail.Core.Models.Attachment? ResolveAttachment(EmailAttachmentRef attachmentRef)
    {
        var path = Path.Combine(_uploadRootPath, attachmentRef.ServerFileName);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Email attachment not found on disk, skipping: {0}", path);
            return null;
        }

        try
        {
            return new FluentEmail.Core.Models.Attachment
            {
                Data = File.OpenRead(path),
                ContentType = GetContentType(attachmentRef.ServerFileName),
                Filename = attachmentRef.DisplayFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read email attachment, skipping: {0}", path);
            return null;
        }
    }

    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}