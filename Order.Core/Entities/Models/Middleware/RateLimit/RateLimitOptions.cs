namespace Order.Core.Entities.Models.Middleware.RateLimit;

public class RateLimitOptions
{
    public List<RateLimitRule> GeneralRules { get; set; }
    public Dictionary<string, List<RateLimitRule>> EndpointRules { get; set; } = new();
    public Dictionary<string, List<RateLimitRule>> PatternRules { get; set; } = new();
    public HashSet<string> WhitelistedIPs { get; set; } = new();
}