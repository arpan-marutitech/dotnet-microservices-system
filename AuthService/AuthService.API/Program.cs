using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AuthService.Application.Services.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AuthService.API.Middleware;
using AuthService.Infrastructure.Security;
using AutoMapper;
using Serilog;
using AuthServiceImpl = AuthService.Application.Services.AuthService;

// -------------------- SERILOG CONFIGURATION --------------------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/authservice-log.txt", rollingInterval: Serilog.RollingInterval.Day)
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
    builder.Services.AddScoped<IAuthService, AuthServiceImpl>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    // JwtHelper
    builder.Services.AddScoped<JwtHelper>();

    // AutoMapper
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // -------------------- JWT AUTH --------------------
    var jwtKey = builder.Configuration["Jwt:Key"] 
        ?? throw new InvalidOperationException("Jwt:Key is not configured.");

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


    // ✅ 🔥 AUTO DATABASE MIGRATION (VERY IMPORTANT)
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

    // Swagger
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