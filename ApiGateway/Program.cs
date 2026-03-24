using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Net;
using System.Net.Sockets;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Udp;
using Serilog.Sinks.Elasticsearch;

// -------------------- SERILOG CONFIGURATION WITH ELASTICSEARCH --------------------
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var logstashIp = Dns.GetHostAddresses("logstash")
    .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
    .ToString();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ApiGateway")
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
        IndexFormat = "logs-apigateway-{0:yyyy.MM.dd}",
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
        "logs/apigateway-{Date}.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}");
}

Log.Logger = loggerConfig.CreateLogger();

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
