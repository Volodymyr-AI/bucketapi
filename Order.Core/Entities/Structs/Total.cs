namespace Order.Core.Entities.Structs;

public struct Total
{
    public decimal Value { get; set; }

    public Total(decimal value)
    {
        if(value < 0)
            throw new ArgumentException("Total cannot be negative");
        Value = value;
    }
    
    public static Total Zero => new Total(0);
    public static Total operator +(Total total, decimal amount) => new Total(total.Value + amount);
}