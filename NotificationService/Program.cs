using MassTransit;
using NotificationService.Services;
using NotificationService.Middleware;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Udp;
using Serilog.Sinks.Elasticsearch;

const string ServiceName = "NotificationService";

// -------------------- SERILOG CONFIGURATION WITH ELASTICSEARCH --------------------
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var logstashIp = Dns.GetHostAddresses("logstash")
    .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
    .ToString();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", ServiceName)
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
        IndexFormat = "logs-notificationservice-{0:yyyy.MM.dd}",
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
        "logs/notificationservice-{Date}.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}");
}

Log.Logger = loggerConfig.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();
    ConfigureTelemetry(builder, ServiceName);

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // -------------------- HEALTH CHECKS --------------------
    builder.Services.AddHealthChecks();

    builder.Services.AddMassTransit(configurator =>
    {
        configurator.SetKebabCaseEndpointNameFormatter();
        configurator.AddConsumer<OrderCreatedConsumer>();

        configurator.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("rabbitmq", "/", host =>
            {
                host.Username("guest");
                host.Password("guest");
            });

            cfg.ReceiveEndpoint("order-created", endpoint =>
            {
                endpoint.ConfigureConsumer<OrderCreatedConsumer>(context);
            });
        });
    });

    var app = builder.Build();

    // -------------------- MIDDLEWARE --------------------
    app.UseMiddleware<ExceptionMiddleware>();

    // Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    // -------------------- HEALTH CHECKS ENDPOINT --------------------
    app.MapHealthChecks("/health");

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", () =>
    {
        var forecast =  Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

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

static void ConfigureTelemetry(WebApplicationBuilder builder, string serviceName)
{
    var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://signoz-otel-collector:4317";
    var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    var processMetrics = new ProcessMetricsCollector(serviceName);

    builder.Services.AddSingleton(processMetrics);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
            }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation(options => options.RecordException = true)
            .AddSource("MassTransit")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("MassTransit")
            .AddMeter(processMetrics.MeterName)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }));
}

internal sealed class ProcessMetricsCollector : IDisposable
{
    private readonly Meter _meter;

    public ProcessMetricsCollector(string serviceName)
    {
        MeterName = $"{serviceName}.Process";
        _meter = new Meter(MeterName);

        _meter.CreateObservableGauge("process.working_set.bytes", () => ReadLong(process => process.WorkingSet64), unit: "By");
        _meter.CreateObservableGauge("process.private_memory.bytes", () => ReadLong(process => process.PrivateMemorySize64), unit: "By");
        _meter.CreateObservableGauge("process.thread.count", () => ReadLong(process => process.Threads.Count));
        _meter.CreateObservableGauge("process.handle.count", ReadHandleCount);
        _meter.CreateObservableGauge("process.cpu.time.total", ReadCpuTime, unit: "s");
    }

    public string MeterName { get; }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static double ReadCpuTime()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime.TotalSeconds;
        }
        catch
        {
            return 0;
        }
    }

    private static long ReadHandleCount()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.HandleCount;
        }
        catch
        {
            return 0;
        }
    }

    private static long ReadLong(Func<Process, long> reader)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            return reader(process);
        }
        catch
        {
            return 0;
        }
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
