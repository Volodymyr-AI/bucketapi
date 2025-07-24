using MediatR;

namespace Order.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<Guid>
{
    public Guid CustomerId { get; set; }
    public List<CreateOrderItemCommand> Items { get; set; } = new();
}