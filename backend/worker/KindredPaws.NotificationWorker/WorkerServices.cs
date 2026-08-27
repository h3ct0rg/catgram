using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using KindredPaws.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
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
    Task SendLikeNotificationAsync(LikeCreatedEvent message, CancellationToken cancellationToken);
    Task SendCommentNotificationAsync(CommentCreatedEvent message, CancellationToken cancellationToken);
    Task SendCommentReplyNotificationAsync(CommentReplyCreatedEvent message, CancellationToken cancellationToken);
    Task SendAdoptionStatusChangedNotificationAsync(AdoptionStatusChangedEvent message, CancellationToken cancellationToken);
    Task SendPostCreatedNotificationAsync(PostCreatedEvent message, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IWorkerEmailSender
{
    public Task SendInvitationAsync(InvitationCreatedEvent message, CancellationToken cancellationToken)
    {
        var link = $"{FrontendBaseUrl}/?invitationToken={Uri.EscapeDataString(message.Token)}";
        return SendAsync(message.Email, "Invitación a Kindred Paws", $"Hola {message.FullName}, completa tu registro aquí: {link}", cancellationToken);
    }

    public Task SendLikeNotificationAsync(LikeCreatedEvent message, CancellationToken cancellationToken) =>
        SendAsync(message.RecipientEmail, "Nuevo like en tu publicación", $"Hola {message.RecipientName}, a alguien le gustó tu publicación: {PostLink(message.PostId)}", cancellationToken);

    public Task SendCommentNotificationAsync(CommentCreatedEvent message, CancellationToken cancellationToken) =>
        SendAsync(message.RecipientEmail, "Nuevo comentario en tu publicación", $"Hola {message.RecipientName}, comentaron tu publicación: \"{message.Excerpt}\". {PostLink(message.PostId)}", cancellationToken);

    public Task SendCommentReplyNotificationAsync(CommentReplyCreatedEvent message, CancellationToken cancellationToken) =>
        SendAsync(message.RecipientEmail, "Nueva respuesta a tu comentario", $"Hola {message.RecipientName}, respondieron tu comentario: \"{message.Excerpt}\".", cancellationToken);

    public Task SendAdoptionStatusChangedNotificationAsync(AdoptionStatusChangedEvent message, CancellationToken cancellationToken) =>
        SendAsync(message.RecipientEmail, "Actualización de adopción", $"Hola {message.RecipientName}, {message.AnimalName} cambió su estado de adopción de {message.OldStatus} a {message.NewStatus}.", cancellationToken);

    public Task SendPostCreatedNotificationAsync(PostCreatedEvent message, CancellationToken cancellationToken) =>
        SendAsync(message.RecipientEmail, "Nueva publicación", $"Hola {message.RecipientName}, {message.AnimalName} tiene una publicación nueva: {PostLink(message.PostId)}", cancellationToken);

    private string FrontendBaseUrl => options.Value.FrontendBaseUrl.TrimEnd('/');
    private string PostLink(Guid postId) => $"{FrontendBaseUrl}/p/{postId}";

    private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            logger.LogWarning("Skipping email '{Subject}': recipient has no email address.", subject);
            return;
        }
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.UserName)) client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        using var mail = new MailMessage(settings.From, toEmail, subject, body);
        await client.SendMailAsync(mail, cancellationToken);
        logger.LogInformation("Email '{Subject}' sent to {Email}", subject, toEmail);
    }
}

/// <summary>
/// Owns a single dedup table in the shared Postgres instance — the worker never reads or writes
/// any of the API's domain tables, only this table, to keep idempotency without crossing the
/// process boundary between the API and the worker.
/// </summary>
public sealed class ProcessedEventStore(IConfiguration configuration, ILogger<ProcessedEventStore> logger)
{
    private string ConnectionString => configuration.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=kindredpaws;Username=kindredpaws;Password=change-me";

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS worker_processed_events (
                    event_id uuid PRIMARY KEY,
                    processed_at timestamptz NOT NULL DEFAULT now()
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not ensure worker_processed_events table exists.");
            throw;
        }
    }

    /// <returns>true if this event id was not seen before (caller should process it); false if it is a duplicate.</returns>
    public async Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO worker_processed_events (event_id) VALUES (@id) ON CONFLICT (event_id) DO NOTHING;";
        command.Parameters.AddWithValue("id", eventId);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}

public sealed class NotificationConsumer(IOptions<RabbitMqOptions> options, IWorkerEmailSender emailSender, ProcessedEventStore processedEvents, ILogger<NotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await processedEvents.EnsureCreatedAsync(stoppingToken);

        var settings = options.Value;
        var deadLetterQueue = $"{settings.NotificationQueue}.dlq";
        var factory = new ConnectionFactory { HostName = settings.HostName, Port = settings.Port, UserName = settings.UserName, Password = settings.Password, VirtualHost = settings.VirtualHost, DispatchConsumersAsync = true };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare(settings.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = string.Empty,
            ["x-dead-letter-routing-key"] = deadLetterQueue,
        });
        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, args) =>
        {
            EventEnvelope? envelope = null;
            try
            {
                envelope = JsonSerializer.Deserialize<EventEnvelope>(Encoding.UTF8.GetString(args.Body.Span)) ?? throw new InvalidOperationException("Invalid event envelope.");
                if (!Guid.TryParse(envelope.EventId, out var eventId)) throw new InvalidOperationException("Invalid event id.");

                var isNew = await processedEvents.TryMarkProcessedAsync(eventId, stoppingToken);
                if (!isNew)
                {
                    logger.LogInformation("Skipping already-processed event {EventId} ({Type})", envelope.EventId, envelope.Type);
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                await DispatchAsync(envelope, stoppingToken);
                channel.BasicAck(args.DeliveryTag, false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification event {EventId} failed; routing to dead-letter queue {Queue}", envelope?.EventId, deadLetterQueue);
                channel.BasicNack(args.DeliveryTag, false, requeue: false);
            }
        };
        channel.BasicConsume(settings.NotificationQueue, autoAck: false, consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task DispatchAsync(EventEnvelope envelope, CancellationToken ct) => envelope.Type switch
    {
        nameof(InvitationCreatedEvent) => HandleAsync<InvitationCreatedEvent>(envelope, emailSender.SendInvitationAsync, ct),
        nameof(LikeCreatedEvent) => HandleAsync<LikeCreatedEvent>(envelope, emailSender.SendLikeNotificationAsync, ct),
        nameof(CommentCreatedEvent) => HandleAsync<CommentCreatedEvent>(envelope, emailSender.SendCommentNotificationAsync, ct),
        nameof(CommentReplyCreatedEvent) => HandleAsync<CommentReplyCreatedEvent>(envelope, emailSender.SendCommentReplyNotificationAsync, ct),
        nameof(AdoptionStatusChangedEvent) => HandleAsync<AdoptionStatusChangedEvent>(envelope, emailSender.SendAdoptionStatusChangedNotificationAsync, ct),
        nameof(PostCreatedEvent) => HandleAsync<PostCreatedEvent>(envelope, emailSender.SendPostCreatedNotificationAsync, ct),
        _ => throw new InvalidOperationException($"Unknown notification event type '{envelope.Type}'."),
    };

    private static Task HandleAsync<T>(EventEnvelope envelope, Func<T, CancellationToken, Task> handler, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<T>(envelope.Payload.ToString()!) ?? throw new InvalidOperationException($"Invalid {typeof(T).Name} payload.");
        return handler(payload, ct);
    }
}
