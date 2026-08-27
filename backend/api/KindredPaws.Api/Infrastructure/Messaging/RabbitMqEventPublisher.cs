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
    private IConnection? connection;
    private IModel? channel;

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        connection ??= factory.CreateConnection();
        channel ??= connection.CreateModel();
        channel.QueueDeclare(queue: options.Value.NotificationQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        var envelope = new EventEnvelope(Guid.NewGuid().ToString(), typeof(T).Name, DateTimeOffset.UtcNow, message!);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        channel.BasicPublish(string.Empty, options.Value.NotificationQueue, properties, body);
        logger.LogDebug("Published notification event {EventId} to {Queue}", envelope.EventId, options.Value.NotificationQueue);
        return Task.CompletedTask;
    }

    public void Dispose() { channel?.Dispose(); connection?.Dispose(); }
}
