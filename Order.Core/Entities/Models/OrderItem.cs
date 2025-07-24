namespace Order.Core.Entities.Models;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    
    // Navigation
    public CustomerOrder CustomerOrder { get; set; }
    
    public decimal LineTotal => Quantity * Price;
}