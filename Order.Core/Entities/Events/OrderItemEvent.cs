using Order.Core.Entities.Abstracts;

namespace Order.Core.Entities.Events;

public class OrderItemEvent : DomainEvent
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal => Quantity * Price;
}