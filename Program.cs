using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();

var app = builder.Build();

// Use CORS policy
app.UseCors("AllowAll");

app.MapControllers();

app.Run();
