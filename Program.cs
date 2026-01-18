using MaltalistApi.Services;
using MaltalistApi.Interfaces;
using MaltalistApi.Middleware;
using MaltalistApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddDatabase();

builder.Services.AddHttpClient();

builder.AddCors();

builder.AddRateLimiter();

builder.Services.AddScoped<IListingsService, ListingsService>();
builder.Services.AddScoped<IPicturesService, PicturesService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportsService, ReportsService>();

builder.AddAuthenticationAndAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseCsrf();
app.MapCsrfTokenEndpoint();

app.MapControllers();

app.Run();
