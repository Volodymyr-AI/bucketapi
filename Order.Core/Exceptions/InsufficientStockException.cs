namespace Order.Core.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(Guid orderItemId, int requestedAmount, int availableAmount) : base(
        "INSUFFICIENT_STOCK",
        $"Product {orderItemId} has only {availableAmount} items, but {requestedAmount} requested")
    {
        
    } 
}