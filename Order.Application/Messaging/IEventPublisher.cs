namespace Order.Application.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, string topicName, CancellationToken cancellationToken = default);
}