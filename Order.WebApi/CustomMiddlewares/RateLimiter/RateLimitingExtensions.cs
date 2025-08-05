using Order.Core.Entities.Models.Middleware.RateLimit;

namespace Order.WebApi.CustomMiddlewares.RateLimiter;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services,
        Action<RateLimitOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddMemoryCache();
        return services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}