using Microsoft.AspNetCore.Antiforgery;

namespace MaltalistApi.Extensions;

public static class CsrfExtensions
{
    /// <summary>
    /// Adds CSRF (Cross-Site Request Forgery) protection middleware.
    /// Validates CSRF tokens for state-changing operations (POST, PUT, DELETE, PATCH).
    /// </summary>
    public static IApplicationBuilder UseCsrf(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            
            // Skip CSRF validation for:
            // 1. GET, HEAD, OPTIONS, TRACE requests (safe methods)
            // 2. The CSRF token endpoint itself
            // 3. Google Auth endpoints (they use OAuth)
            if (method == "GET" || method == "HEAD" || method == "OPTIONS" || method == "TRACE" ||
                path.Contains("/csrf-token", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/googleauth", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }
            
            // Validate CSRF token for POST, PUT, DELETE, PATCH
            var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            
            try
            {
                await antiforgery.ValidateRequestAsync(context);
                logger.LogInformation("CSRF validation passed for {Method} {Path}", method, path);
                await next();
            }
            catch (AntiforgeryValidationException ex)
            {
                logger.LogWarning("CSRF validation failed for {Method} {Path}: {Error}", method, path, ex.Message);
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing CSRF token" });
            }
        });

        return app;
    }

    /// <summary>
    /// Maps the CSRF token endpoint that clients use to retrieve CSRF tokens.
    /// </summary>
    public static IEndpointRouteBuilder MapCsrfTokenEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/csrf-token", [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken] 
            (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            // The cookie is automatically set by GetAndStoreTokens
            // We just return the request token for the client to use in headers
            return Results.Ok(new { token = tokens.RequestToken });
        })
        .RequireCors("AllowSpecificOrigins");

        return endpoints;
    }
}
