using MaltalistApi.Models;
using MaltalistApi.Services;
using MaltalistApi.Interfaces;
using MaltalistApi.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MaltalistDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(3)));

builder.Services.AddHttpClient();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost", "http://localhost:80" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization", "X-XSRF-TOKEN")
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
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

    options.AddFixedWindowLimiter("auth", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
    };
});

builder.Services.AddScoped<IListingsService, ListingsService>();
builder.Services.AddScoped<IPicturesService, PicturesService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/googleauth/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(12); // Reduced from 7 days to 12 hours
        options.SlidingExpiration = true;
        
        // Enhanced security settings
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Always in production
        options.Cookie.SameSite = SameSiteMode.Strict;
        
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Add Antiforgery services for CSRF protection
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.HttpOnly = false; // Must be false so JavaScript can read it
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddControllers();

var app = builder.Build();

// Add error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Add CSRF validation middleware for state-changing operations
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
    var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        await antiforgery.ValidateRequestAsync(context);
        logger.LogInformation("CSRF validation passed for {Method} {Path}", method, path);
        await next();
    }
    catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException ex)
    {
        logger.LogWarning("CSRF validation failed for {Method} {Path}: {Error}", method, path, ex.Message);
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing CSRF token" });
    }
});

// CSRF token endpoint - allows clients to get the token
app.MapGet("/api/csrf-token", [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken] 
    (Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    // The cookie is automatically set by GetAndStoreTokens
    // We just return the request token for the client to use in headers
    return Results.Ok(new { token = tokens.RequestToken });
})
.RequireCors("AllowSpecificOrigins");

app.MapControllers();

app.Run();
