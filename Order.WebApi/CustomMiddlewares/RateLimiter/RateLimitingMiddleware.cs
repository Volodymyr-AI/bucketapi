using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Order.Core.Entities.Models.Middleware.RateLimit;

namespace Order.WebApi.CustomMiddlewares.RateLimiter;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly RateLimitOptions _options;

    public RateLimitingMiddleware(
        RequestDelegate next, 
        ILogger<RateLimitingMiddleware> logger, 
        IMemoryCache cache,
        IOptions<RateLimitOptions> options)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipRateLimit(context))
        {
            await _next(context);
            return;
        }
        
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);
        
        var rules = GetRateLimitRules(endpoint);

        foreach (var rule in rules)
        {
            var key = $"rate_limit:{rule.Name}:{clientId}";

            if (!await IsRequestAllowedAsync(key, rule))
            {
                await HandleRateLimitExceededAsync(context, rule, clientId);
                return;
            }
        }

        await AddRateLimitHeaders(context, clientId, rules);
        
        await _next(context);
    }

    private bool ShouldSkipRateLimit(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        var skipPaths = new[] {"/health", "/metrics", "favicon.ico"};
        if(skipPaths.Any(p => path.StartsWith(p)))
            return true;
        
        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".gif", ".ico", ".svg" };
        if(staticExtensions.Any(ext => path.EndsWith(ext)))
            return true;
        
        var clientIp = GetClientIP(context);
        if(_options.WhitelistedIPs.Contains(clientIp))
            return true;
        
        return false;
    }

    private string GetClientIdentifier(HttpContext context)
    {
        var ip = GetClientIP(context);
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.Identity?.Name;

        return string.IsNullOrEmpty(userId) ? ip : $"{userId}:{ip}";
    }

    private string GetClientIP(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress.ToString() ?? "unknown";
    }

    private string GetEndpointKey(HttpContext context)
    {
        return $"{context.Request.Method}:{context.Request.Path}";
    }

    private List<RateLimitRule> GetRateLimitRules(string endpoint)
    {
        if(_options.EndpointRules.TryGetValue(endpoint, out var rule))
            return rule;

        foreach (var patternRule in _options.EndpointRules)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(endpoint, patternRule.Key))
                return patternRule.Value;
        }
        
        return _options.GeneralRules;
    }

    private async Task<bool> IsRequestAllowedAsync(string key, RateLimitRule rule)
    {
        return await Task.Run(() =>
        {
            var bucket = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = rule.Window;
                return new TokenBucket(rule.Limit, rule.Window);
            });

            return bucket.TryConsume();
        });
    }

    private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitRule rule, string clientId)
    {
        var retryAfter = rule.Window.TotalSeconds;
        
        _logger.LogWarning(
            "Rate limit exceeded for client {ClientId}. Rule: {RuleName}, Limit: {Limit}, Window: {Window}",
            clientId, rule.Name, rule.Limit, rule.Window);
        
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.Headers.Add("Retry-After", retryAfter.ToString());
        context.Response.Headers.Add("X-RateLimit-Limit", rule.Limit.ToString());
        context.Response.Headers.Add("X-RateLimit-Remaining", "0");
        context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.Add(rule.Window).ToUnixTimeSeconds().ToString());
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Too Many Requests",
            message = $"Rate limit exceeded. Try again in {retryAfter} seconds.",
            retryAfter = retryAfter
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
    
    private async Task AddRateLimitHeaders(HttpContext context, string clientId, List<RateLimitRule> rules)
    {
        var primaryRule = rules.OrderBy(r => r.Limit).First();
        var key = $"rate_limit:{primaryRule.Name}:{clientId}";
            
        await Task.Run(() =>
        {
            if (_cache.TryGetValue(key, out TokenBucket bucket))
            {
                context.Response.Headers.Add("X-RateLimit-Limit", primaryRule.Limit.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", bucket.Tokens.ToString());
                context.Response.Headers.Add("X-RateLimit-Reset", 
                    DateTimeOffset.UtcNow.Add(primaryRule.Window).ToUnixTimeSeconds().ToString());
            }
        });
    }
}    