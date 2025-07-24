using Order.Core.Entities.Abstracts;
using Order.Core.Entities.Models;

namespace Order.Core.Entities.Events;

public class OrderCreatedEvent : DomainEvent
{
    public string Id { get; private set; }
    public string CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<OrderItemEvent> Items { get; private set; }
    
    public OrderCreatedEvent(
        Guid orderId, 
        Guid customerId,
        decimal amount,
        DateTime createdAt, 
        IEnumerable<OrderItem> orderItems)
    {
        Id = orderId.ToString();
        CustomerId = customerId.ToString();
        Amount = amount;
        CreatedAt = createdAt;
        
        Items = orderItems.Select(item => new OrderItemEvent
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();
    }
}