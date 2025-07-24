using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Order.Core.Entities.Abstracts;
using Order.Core.Entities.Enums;
using Order.Core.Entities.Events;
using Order.Core.Entities.Structs;

namespace Order.Core.Entities.Models;

public class CustomerOrder : Entity
{
    [Key]
    public Guid CustomerOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; private set; }
    public decimal TotalValue { get; private set; }

    [NotMapped]
    public Total Total
    {
        get => new Total(TotalValue);
        set => TotalValue = value.Value;
    }
    public OrderStatus Status { get; set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public void Create()
    {
        ValidateForCreation();
        
        Status = OrderStatus.Created;
        CreatedAt = DateTime.UtcNow;

        RecalculateTotal();

        Raise(new OrderCreatedEvent(CustomerOrderId, CustomerId, Total.Value, CreatedAt, Items));
    }

    public void AddItem(string productId, string productName, int quantity, decimal price)
    {
        if(Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add item to non-pending order");

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = CustomerOrderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            Price = price
        };
        _items.Add(item);
        RecalculateTotal();
    }
    
    public void RemoveItem(Guid itemId)
    {
        if(Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove item from non-pending order");
        
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    private void ValidateForCreation()
    {
        if(!_items.Any())
            throw new InvalidOperationException("Cannot create order without any items");
        if(_items.Any(item => item.Quantity <= 0))
            throw new InvalidOperationException("Quantity of an item cannot be less or equal to zero");
        if(_items.Any(item => item.Price < 0))
            throw new InvalidOperationException("Price cannot be less than zero");
    }
    private void RecalculateTotal()
    {
        var totalAmount = _items.Sum(i => i.LineTotal);
        Total = new Total(totalAmount);
    }
    public bool IsTimedOut(TimeSpan timeout) => DateTime.UtcNow - CreatedAt > timeout;
}