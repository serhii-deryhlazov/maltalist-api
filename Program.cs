using MaltalistApi.Models;
using MaltalistApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MaltalistDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IListingsService, ListingsService>();
builder.Services.AddScoped<IPicturesService, PicturesService>();
builder.Services.AddScoped<IUsersService, UsersService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();
