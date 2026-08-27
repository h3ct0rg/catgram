using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using KindredPaws.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace KindredPaws.NotificationWorker;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string NotificationQueue { get; set; } = "kindred-paws.notifications";
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "no-reply@kindredpaws.local";
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}

public interface IWorkerEmailSender
{
    Task SendInvitationAsync(InvitationCreatedEvent message, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IWorkerEmailSender
{
    public async Task SendInvitationAsync(InvitationCreatedEvent message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.UserName)) client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        var link = $"{settings.FrontendBaseUrl.TrimEnd('/')}/?invitationToken={Uri.EscapeDataString(message.Token)}";
        using var mail = new MailMessage(settings.From, message.Email, "Invitación a Kindred Paws", $"Hola {message.FullName}, completa tu registro aquí: {link}");
        await client.SendMailAsync(mail, cancellationToken);
        logger.LogInformation("Invitation email sent to {Email}", message.Email);
    }
}

public sealed class NotificationConsumer(IOptions<RabbitMqOptions> options, IWorkerEmailSender emailSender, ILogger<NotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory { HostName = settings.HostName, Port = settings.Port, UserName = settings.UserName, Password = settings.Password, VirtualHost = settings.VirtualHost, DispatchConsumersAsync = true };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(settings.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.BasicQos(0, 1, false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(Encoding.UTF8.GetString(args.Body.Span)) ?? throw new InvalidOperationException("Invalid event envelope.");
                if (envelope.Type == nameof(InvitationCreatedEvent))
                {
                    var payload = JsonSerializer.Deserialize<InvitationCreatedEvent>(envelope.Payload.ToString()!) ?? throw new InvalidOperationException("Invalid invitation event.");
                    await emailSender.SendInvitationAsync(payload, stoppingToken);
                }
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification event failed; message will be requeued");
                channel.BasicNack(args.DeliveryTag, false, requeue: true);
            }
        };
        channel.BasicConsume(settings.NotificationQueue, autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
