using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace MaltalistApi.Extensions;

public static class RateLimiterExtensions
{
    /// <summary>
    /// Adds rate limiting with global and authentication-specific limits.
    /// Global: 100 requests per minute per IP.
    /// Auth: 10 requests per minute per IP.
    /// </summary>
    public static IServiceCollection AddRateLimiter(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            // Global rate limiter - 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Auth-specific rate limiter - 10 requests per minute per IP
            options.AddFixedWindowLimiter("auth", options =>
            {
                options.PermitLimit = 10;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 0;
            });

            // Custom response when rate limit is exceeded
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
            };
        });

        return builder.Services;
    }
}
