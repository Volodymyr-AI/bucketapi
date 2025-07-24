using Order.Application.Orders.Commands.CreateOrder;
using Order.Infrastructure.DTO;

namespace Order.Database.DTO;

public class CreateCustomerOrderDTO
{
    public Guid CustomerId { get; set; } = default!;
    public List<CreateOrderItemDTO> Items { get; set; } = new();

    public CreateOrderCommand ToCommand()
    {
        return new CreateOrderCommand
        {
            CustomerId = this.CustomerId,
            Items = this.Items.Select(i => new CreateOrderItemCommand
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
    }
}