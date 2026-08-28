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
    Task SendAdoptionRequestStatusChangedNotificationAsync(AdoptionRequestStatusChangedEvent message, CancellationToken cancellationToken);
    Task SendNewAdoptionRequestNotificationAsync(NewAdoptionRequestEvent message, CancellationToken cancellationToken);
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IWorkerEmailSender
{
    public Task SendInvitationAsync(InvitationCreatedEvent message, CancellationToken cancellationToken)
    {
        var link = $"{FrontendBaseUrl}/?invitationToken={Uri.EscapeDataString(message.Token)}";
        var body = $"""
            <p>Hola {Encode(message.FullName)},</p>
            <p>Te invitaron a unirte a <strong>Kindred Paws</strong>, la plataforma donde refugios y familias se encuentran para que ningún animal se quede sin un hogar.</p>
            <p>Completa tu registro para empezar a ayudar a los animalitos a encontrar una nueva familia.</p>
            """;
        return SendAsync(message.Email, "🐾 Te invitaron a Kindred Paws", EmailTemplate.Build("Una invitación con patitas", body, "Completar registro", link), cancellationToken);
    }

    public Task SendLikeNotificationAsync(LikeCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p>A alguien le encantó tu publicación y le dio <strong>like</strong>. Cada corazón acerca un poco más a un animalito a su nuevo hogar.</p>
            """;
        return SendAsync(message.RecipientEmail, "❤️ Nuevo like en tu publicación", EmailTemplate.Build("¡Tienes un nuevo like!", body, "Ver publicación", PostLink(message.PostId)), cancellationToken);
    }

    public Task SendCommentNotificationAsync(CommentCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p>Comentaron tu publicación:</p>
            {Quote(message.Excerpt)}
            """;
        return SendAsync(message.RecipientEmail, "💬 Nuevo comentario en tu publicación", EmailTemplate.Build("Tienes un nuevo comentario", body, "Ver publicación", PostLink(message.PostId)), cancellationToken);
    }

    public Task SendCommentReplyNotificationAsync(CommentReplyCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p>Respondieron tu comentario:</p>
            {Quote(message.Excerpt)}
            """;
        return SendAsync(message.RecipientEmail, "💬 Nueva respuesta a tu comentario", EmailTemplate.Build("Tienes una nueva respuesta", body, "Abrir Kindred Paws", FrontendBaseUrl), cancellationToken);
    }

    public Task SendAdoptionStatusChangedNotificationAsync(AdoptionStatusChangedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p><strong>{Encode(message.AnimalName)}</strong> cambió su estado de adopción de <em>{Encode(message.OldStatus)}</em> a <strong>{Encode(message.NewStatus)}</strong>.</p>
            <p>Gracias por seguir su historia y ayudarlo a encontrar un hogar para siempre.</p>
            """;
        return SendAsync(message.RecipientEmail, $"🐾 Actualización de adopción de {message.AnimalName}", EmailTemplate.Build("Actualización de adopción", body, $"Ver a {message.AnimalName}", AnimalLink(message.AnimalId)), cancellationToken);
    }

    public Task SendPostCreatedNotificationAsync(PostCreatedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p><strong>{Encode(message.AnimalName)}</strong> tiene una publicación nueva. ¡Ve a ver las novedades!</p>
            """;
        return SendAsync(message.RecipientEmail, $"📸 Nueva publicación de {message.AnimalName}", EmailTemplate.Build("Nueva publicación", body, "Ver publicación", PostLink(message.PostId)), cancellationToken);
    }

    public Task SendAdoptionRequestStatusChangedNotificationAsync(AdoptionRequestStatusChangedEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p>Tu solicitud para adoptar a <strong>{Encode(message.AnimalName)}</strong> cambió a: <strong>{Encode(message.Status)}</strong>.</p>
            <p>Gracias por abrirle las puertas de tu hogar a un animalito que lo necesita.</p>
            """;
        return SendAsync(message.RecipientEmail, $"🏡 Actualización de tu solicitud por {message.AnimalName}", EmailTemplate.Build("Actualización de tu solicitud", body, $"Ver a {message.AnimalName}", AnimalLink(message.AnimalId)), cancellationToken);
    }

    public Task SendNewAdoptionRequestNotificationAsync(NewAdoptionRequestEvent message, CancellationToken cancellationToken)
    {
        var body = $"""
            <p>Hola {Encode(message.RecipientName)},</p>
            <p><strong>{Encode(message.ApplicantName)}</strong> quiere darle un hogar a <strong>{Encode(message.AnimalName)}</strong>.</p>
            <p>Ingresa al panel de solicitudes de tu refugio para comenzar el proceso de adopción.</p>
            """;
        return SendAsync(message.RecipientEmail, $"🐕 Nueva solicitud de adopción por {message.AnimalName}", EmailTemplate.Build("Nueva solicitud de adopción", body, "Ver solicitudes", $"{FrontendBaseUrl}/admin/adoptions"), cancellationToken);
    }

    private string FrontendBaseUrl => options.Value.FrontendBaseUrl.TrimEnd('/');
    private string PostLink(Guid postId) => $"{FrontendBaseUrl}/p/{postId}";
    private string AnimalLink(Guid animalId) => $"{FrontendBaseUrl}/animals/{animalId}";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
    private static string Quote(string excerpt) =>
        $"""<blockquote style="margin:12px 0;padding:12px 16px;background:#f5f7fb;border-left:3px solid #2e5bff;border-radius:8px;color:#414754;font-style:italic;">&ldquo;{Encode(excerpt)}&rdquo;</blockquote>""";

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            logger.LogWarning("Skipping email '{Subject}': recipient has no email address.", subject);
            return;
        }
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.UserName)) client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        using var mail = new MailMessage(settings.From, toEmail, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(mail, cancellationToken);
        logger.LogInformation("Email '{Subject}' sent to {Email}", subject, toEmail);
    }
}

/// <summary>
/// Shared branded shell for every outgoing email. Uses inline styles and a table-based layout
/// (no external CSS/fonts) because most email clients strip &lt;style&gt; blocks and cannot load
/// web fonts reliably — the closest safe approximation of the platform's blue/glass identity.
/// </summary>
internal static class EmailTemplate
{
    public static string Build(string heading, string bodyHtml, string? ctaText = null, string? ctaUrl = null)
    {
        var cta = string.IsNullOrWhiteSpace(ctaText) || string.IsNullOrWhiteSpace(ctaUrl)
            ? string.Empty
            : $"""
                <div style="text-align:center;margin-top:28px;">
                  <a href="{ctaUrl}" style="display:inline-block;background:#2e5bff;color:#ffffff;text-decoration:none;font-weight:700;font-size:14px;padding:12px 28px;border-radius:999px;">{WebUtility.HtmlEncode(ctaText)}</a>
                </div>
                """;

        return $"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>Kindred Paws</title>
            </head>
            <body style="margin:0;padding:0;background:#eef3fb;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#eef3fb;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="100%" style="max-width:480px;background:#ffffff;border-radius:20px;overflow:hidden;box-shadow:0 12px 30px rgba(0,89,187,0.12);">
                      <tr>
                        <td style="background:linear-gradient(135deg,#0059bb,#2e5bff);padding:28px 32px;text-align:center;">
                          <div style="font-size:28px;line-height:1;">🐾</div>
                          <div style="color:#ffffff;font-size:20px;font-weight:800;letter-spacing:-0.02em;margin-top:8px;">Kindred Paws</div>
                          <div style="color:rgba(255,255,255,0.85);font-size:12px;margin-top:4px;">Un hogar empieza aquí</div>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:32px;">
                          <h1 style="margin:0 0 12px;font-size:20px;color:#181c23;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">{WebUtility.HtmlEncode(heading)}</h1>
                          <div style="font-size:14px;line-height:1.6;color:#414754;">
                            {bodyHtml}
                          </div>
                          {cta}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:20px 32px;background:#f5f7fb;text-align:center;">
                          <div style="font-size:12px;color:#717786;">Ayudamos a que los animales de refugio encuentren un hogar para siempre. 🐾</div>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
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
        var settings = options.Value;
        var deadLetterQueue = $"{settings.NotificationQueue}.dlq";
        var factory = new ConnectionFactory { HostName = settings.HostName, Port = settings.Port, UserName = settings.UserName, Password = settings.Password, VirtualHost = settings.VirtualHost, DispatchConsumersAsync = true };

        // A BackgroundService that throws out of ExecuteAsync takes down the whole worker process
        // (BackgroundServiceExceptionBehavior.StopHost is the default) and never recovers, even once
        // RabbitMQ/Postgres come back. Loop and reconnect with backoff instead of letting that happen.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await processedEvents.EnsureCreatedAsync(stoppingToken);

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
                logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, listening on {Queue}.", settings.HostName, settings.Port, settings.NotificationQueue);

                // Block here until the connection drops or the host asks us to stop; then loop and reconnect.
                var connectionLost = new TaskCompletionSource();
                connection.ConnectionShutdown += (_, _) => connectionLost.TrySetResult();
                using var registration = stoppingToken.Register(() => connectionLost.TrySetResult());
                await connectionLost.Task;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Notification worker could not reach RabbitMQ or Postgres; retrying in 10 seconds.");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private Task DispatchAsync(EventEnvelope envelope, CancellationToken ct) => envelope.Type switch
    {
        nameof(InvitationCreatedEvent) => HandleAsync<InvitationCreatedEvent>(envelope, emailSender.SendInvitationAsync, ct),
        nameof(LikeCreatedEvent) => HandleAsync<LikeCreatedEvent>(envelope, emailSender.SendLikeNotificationAsync, ct),
        nameof(CommentCreatedEvent) => HandleAsync<CommentCreatedEvent>(envelope, emailSender.SendCommentNotificationAsync, ct),
        nameof(CommentReplyCreatedEvent) => HandleAsync<CommentReplyCreatedEvent>(envelope, emailSender.SendCommentReplyNotificationAsync, ct),
        nameof(AdoptionStatusChangedEvent) => HandleAsync<AdoptionStatusChangedEvent>(envelope, emailSender.SendAdoptionStatusChangedNotificationAsync, ct),
        nameof(PostCreatedEvent) => HandleAsync<PostCreatedEvent>(envelope, emailSender.SendPostCreatedNotificationAsync, ct),
        nameof(AdoptionRequestStatusChangedEvent) => HandleAsync<AdoptionRequestStatusChangedEvent>(envelope, emailSender.SendAdoptionRequestStatusChangedNotificationAsync, ct),
        nameof(NewAdoptionRequestEvent) => HandleAsync<NewAdoptionRequestEvent>(envelope, emailSender.SendNewAdoptionRequestNotificationAsync, ct),
        _ => throw new InvalidOperationException($"Unknown notification event type '{envelope.Type}'."),
    };

    private static Task HandleAsync<T>(EventEnvelope envelope, Func<T, CancellationToken, Task> handler, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<T>(envelope.Payload.ToString()!) ?? throw new InvalidOperationException($"Invalid {typeof(T).Name} payload.");
        return handler(payload, ct);
    }
}
