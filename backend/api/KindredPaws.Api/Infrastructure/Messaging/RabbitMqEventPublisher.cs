using System.Text;
using System.Text.Json;
using KindredPaws.Api.Application.Shared;
using KindredPaws.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace KindredPaws.Api.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IDisposable
{
    private readonly ConnectionFactory factory = new() { HostName = options.Value.HostName, Port = options.Value.Port, UserName = options.Value.UserName, Password = options.Value.Password, VirtualHost = options.Value.VirtualHost, DispatchConsumersAsync = true };
    private readonly object gate = new();
    private IConnection? connection;
    private IModel? channel;

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // RabbitMQ only carries secondary email notifications here — a broker outage must never
        // fail the primary action (like/comment/post/invite/adoption request) that already
        // committed to the database. Swallow and log instead of throwing.
        try
        {
            lock (gate)
            {
                connection ??= factory.CreateConnection();
                channel ??= connection.CreateModel();
                // Must match the worker's queue declaration exactly (including the dead-letter
                // arguments) — RabbitMQ rejects a redeclare with different arguments for the same
                // queue name, regardless of which side (API or worker) happens to declare it first.
                var deadLetterQueue = $"{options.Value.NotificationQueue}.dlq";
                channel.QueueDeclare(queue: deadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: options.Value.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = string.Empty,
                    ["x-dead-letter-routing-key"] = deadLetterQueue,
                });
                var envelope = new EventEnvelope(Guid.NewGuid().ToString(), typeof(T).Name, DateTimeOffset.UtcNow, message!);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                channel.BasicPublish(string.Empty, options.Value.NotificationQueue, properties, body);
                logger.LogDebug("Published notification event {EventId} to {Queue}", envelope.EventId, options.Value.NotificationQueue);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not publish {EventType} to RabbitMQ; the event was dropped and no email notification will be sent for it.", typeof(T).Name);
            lock (gate)
            {
                channel = null;
                connection = null;
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose() { channel?.Dispose(); connection?.Dispose(); }
}
