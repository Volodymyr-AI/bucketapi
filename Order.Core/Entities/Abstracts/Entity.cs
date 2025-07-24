namespace Order.Core.Entities.Abstracts;

public abstract class Entity
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents;

    protected void Raise(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public IReadOnlyCollection<DomainEvent> GetDomainEvents() => DomainEvents;

    public void RemoveDomainEvent(Guid eventId) => _domainEvents.FirstOrDefault(x => x.EventId == eventId);
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}