using Order.Application.Orders.Commands.CreateOrder;

namespace Order.Infrastructure.DTO;

public class CreateOrderItemDTO
{
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public CreateOrderItemCommand ToCommand()
    {
        return new CreateOrderItemCommand()
        {
            ProductId = this.ProductId,
            ProductName = this.ProductName,
            Quantity = this.Quantity,
            Price = this.Price
        };
    }
}