namespace MaltalistApi.Extensions;

public static class CorsExtensions
{
    /// <summary>
    /// Adds CORS policy that allows credentials and specific origins from configuration.
    /// Reads allowed origins from "AllowedOrigins" section in appsettings.json.
    /// </summary>
    public static IServiceCollection AddCors(this WebApplicationBuilder builder)
    {
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

        return builder.Services;
    }
}
