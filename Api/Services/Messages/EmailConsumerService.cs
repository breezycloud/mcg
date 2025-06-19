using System.Text;
using System.Text.Json;
using FluentEmail.Core;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;
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
    private readonly string _templatePath;

    public EmailConsumerService(
        IOptions<MessageBrokerSetting> rabbitConfig,
        IFluentEmail fluentEmail,
        ILogger<EmailConsumerService> logger)
    {
        _fluentEmail = fluentEmail;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = rabbitConfig.Value.HostName,
            UserName = rabbitConfig.Value.UserName,
            Password = rabbitConfig.Value.Password,
            Port = rabbitConfig.Value.Port
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Set up RazorLight
        // _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates");
        // _razorEngine = new RazorLightEngineBuilder()
        //     .UseFileSystemProject(_templatePath)
        //     .UseMemoryCachingProvider()
        //     .Build();
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

    bool HasAttachment = false;
    private async Task ProcessEmailMessageAsync(EmailQueueMessage message)
    {
        dynamic model;
        var templatePath = $"{message.Template}.cshtml";
        var json = JsonSerializer.Serialize(message.TemplateModel);
        _logger.LogInformation("Processing email template {0} with model {1}", templatePath, json);
        // if (!templatePath.Contains("AccountDetail", StringComparison.OrdinalIgnoreCase))
        model = JsonSerializer.Deserialize<AccountDetailBody>(json);
        // else
        // {
        //     model = JsonSerializer.Deserialize<BookingConfirmation>(json);
        //     HasAttachment = true;
        // }
        var htmlContent = await _razorEngine.CompileRenderAsync(templatePath, model);
        FluentEmail.Core.Models.Attachment? attachment = null;
        if (HasAttachment)
        {
            attachment = await PrepareAttachment(htmlContent);
        }

        var email = _fluentEmail
            .To(message.To)
            .Subject(message.Subject)
            .Body(htmlContent, true);

        if (attachment != null)
        {
            email.Attach(attachment);
        }

        await email.SendAsync();
        _logger.LogInformation("Mail successfully sent to {0}", message!.To);
    }

    private async Task<FluentEmail.Core.Models.Attachment?> PrepareAttachment(string content)
    {
        FluentEmail.Core.Models.Attachment? attachment;
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"]
        });
        using (var page = await browser.NewPageAsync())
        {
            await page.SetContentAsync(content);
            var pdfStream = await page.PdfStreamAsync(new PdfOptions
            {
                PrintBackground = true
            });
            attachment = new FluentEmail.Core.Models.Attachment
            {
                Data = pdfStream,
                ContentType = "application/pdf",
                Filename = "Your Booking.pdf"
            };
        }
        return attachment;
    }




    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}