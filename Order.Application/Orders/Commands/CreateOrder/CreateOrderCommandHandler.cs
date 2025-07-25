using MediatR;
using Order.Application.Database;
using Order.Application.Messaging;
using Order.Core.Entities.Enums;
using Order.Core.Entities.Events;
using Order.Core.Entities.Models;

namespace Order.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderDbContext _orderDbContext;
    private readonly IEventPublisher _eventPublisher;

    public CreateOrderCommandHandler(IOrderDbContext orderDbContext, IEventPublisher eventPublisher)
    {
        _orderDbContext = orderDbContext;
        _eventPublisher = eventPublisher;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Create new order
        var order = new CustomerOrder
        {
            CustomerOrderId = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending
        };

        // Add into items
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.Quantity, item.Price);
        }
        
        // Create domain event
        order.Create();
        
        // Save into db
        await _orderDbContext.CustomerOrders.AddAsync(order, cancellationToken);
        await _orderDbContext.SaveChangesAsync(cancellationToken);
        Console.WriteLine("_orderDbContext.GetType(): " + _orderDbContext.GetType());
        // Publish event
        await PublishDomainEventAsync(order);
        
        return order.CustomerOrderId;
    }

    private async Task PublishDomainEventAsync(CustomerOrder order)
    {
        foreach (var domainEvent in order.GetDomainEvents())
        {
            if (domainEvent is OrderCreatedEvent orderCreated)
            {
                // Publicate into Kafka
                await _eventPublisher.PublishAsync(orderCreated, "order-created");
            }
        }
        
        // Clear after publication
        order.ClearDomainEvents();
    }
}