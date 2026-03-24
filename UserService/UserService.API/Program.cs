using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading;
using StackExchange.Redis;
using UserService.Application.Common;
using UserService.Application.Services;
using UserService.Application.Services.Interfaces;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;
using UserService.API.Middleware;
using System.Net;
using System.Net.Sockets;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Udp;
using Serilog.Sinks.Elasticsearch;
using UserServiceImplementation = UserService.Application.Services.UserService;

// -------------------- SERILOG CONFIGURATION WITH ELASTICSEARCH --------------------
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var logstashIp = Dns.GetHostAddresses("logstash")
    .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
    .ToString();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "UserService")
    .Enrich.WithProperty("Environment", environment)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Udp(
            logstashIp,
            5000,
            outputTemplate: "lvl={Level:u3}|svc={Service}|env={Environment}|msg={Message:lj}|props={Properties:j}|ex={Exception}")
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
        IndexFormat = "logs-userservice-{0:yyyy.MM.dd}",
        BufferBaseFilename = null,
        BufferFileSizeLimitBytes = 52428800,
        BufferLogShippingInterval = TimeSpan.FromSeconds(30),
        ModifyConnectionSettings = cfg =>
        {
            cfg.BasicAuthentication("elastic", "elastic123");
            cfg.DisablePing();
            return cfg;
        }
    });

// Add file logging only in Development
if (environment == "Development")
{
    loggerConfig = loggerConfig.WriteTo.File(
        "logs/userservice-{Date}.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}");
}

Log.Logger = loggerConfig.CreateLogger();

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
    builder.Services.AddScoped<IUserService, UserServiceImplementation>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // AutoMapper
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // Redis
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect("redis:6379")
    );
    builder.Services.AddScoped<RedisService>();

    // -------------------- JWT AUTH --------------------
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

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
