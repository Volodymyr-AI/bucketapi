namespace Order.Core.Entities.Models.Middleware.RateLimit;

public class RateLimitRule
{
    public string Name { get; set; } = "";
    public int Limit { get; set; }
    public TimeSpan Window { get; set; } 
}