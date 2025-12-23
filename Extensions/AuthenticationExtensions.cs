using Microsoft.AspNetCore.Authentication.Cookies;

namespace MaltalistApi.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds cookie-based authentication and authorization services with enhanced security settings.
    /// </summary>
    public static IServiceCollection AddAuthenticationAndAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/api/googleauth/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
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

        return builder.Services;
    }
}
