using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading;
using OrderService.Application.Clients;
using OrderService.Application.Messaging;
using OrderService.Application.Services.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OrderService.API.Middleware;
using StackExchange.Redis;
using OrderService.Application.Common;
using AutoMapper;
using Serilog;
using OrderServiceImpl = OrderService.Application.Services.OrderService;

// -------------------- SERILOG CONFIGURATION --------------------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderservice-log.txt", rollingInterval: Serilog.RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // -------------------- DATABASE --------------------
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null
                );
            }
        ));

    // -------------------- CONTROLLERS --------------------
    builder.Services.AddControllers();

    // -------------------- SWAGGER / OPENAPI --------------------
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // -------------------- HEALTH CHECKS --------------------
    builder.Services.AddHealthChecks();

    // -------------------- DEPENDENCY INJECTION --------------------
    var userServiceBaseUrl = builder.Configuration["UserService:BaseUrl"] ?? "https://localhost:5244";

    builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
    {
        client.BaseAddress = new Uri(userServiceBaseUrl);
    });

    builder.Services.AddScoped<IOrderService, OrderServiceImpl>();

    // RabbitMQ publisher used to emit events (e.g., OrderCreated) for other services to consume.
    builder.Services.AddSingleton<RabbitMqPublisher>();

    builder.Services.AddScoped<IOrderRepository, OrderRepository>();

    // AutoMapper
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // Redis
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect("redis:6379")
    );
    builder.Services.AddScoped<RedisService>();

    // -------------------- JWT AUTH --------------------
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
    var key = Encoding.UTF8.GetBytes(jwtKey);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,  
                ValidateAudience = false, 
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

    builder.Services.AddAuthorization();

    // -------------------- BUILD APP --------------------
    var app = builder.Build();

    // -------------------- AUTO DATABASE MIGRATION --------------------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var retries = 5;
        while (retries > 0)
        {
            try
            {
                db.Database.Migrate();
                break;
            }
            catch
            {
                retries--;
                Thread.Sleep(5000);
            }
        }
    }

    // -------------------- MIDDLEWARE --------------------
    app.UseMiddleware<ExceptionMiddleware>();

    // -------------------- HEALTH CHECKS ENDPOINT --------------------
    app.MapHealthChecks("/health");

    // Swagger/OpenAPI
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during startup");
}
finally
{
    Log.CloseAndFlush();
}