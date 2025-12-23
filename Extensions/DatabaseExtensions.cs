using MaltalistApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MaltalistApi.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Adds MySQL database context with automatic retry on failure.
    /// </summary>
    public static IServiceCollection AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<MaltalistDbContext>(options =>
            options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
                mysqlOptions => mysqlOptions.EnableRetryOnFailure(3)));

        return builder.Services;
    }
}
