namespace Order.Core.Entities.Abstracts;

public abstract class DomainEvent
{
    public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
    public Guid EventId { get; private set; } = Guid.NewGuid();
}