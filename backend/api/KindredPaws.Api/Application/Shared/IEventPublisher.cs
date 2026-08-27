namespace KindredPaws.Api.Application.Shared;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken);
}
