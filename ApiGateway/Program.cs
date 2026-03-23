using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

// -------------------- SERILOG CONFIGURATION --------------------
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/apigateway-log.txt", rollingInterval: Serilog.RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddOcelot();
    
    // -------------------- HEALTH CHECKS --------------------
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseHttpsRedirection();

    app.UseSwagger();
    app.UseSwaggerUI();

    // -------------------- HEALTH CHECKS ENDPOINT --------------------
    app.MapHealthChecks("/health");

    await app.UseOcelot();

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
