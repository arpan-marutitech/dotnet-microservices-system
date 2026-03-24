// ============================================================================
// SERILOG CONFIGURATION HELPER
// ============================================================================
// Provides a centralized way to configure Serilog with Elasticsearch sink
// for all microservices with consistent settings.
// ============================================================================

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System;
using System.Collections.Generic;

namespace SharedLogging.Configuration
{
    /// <summary>
    /// Configuration helper for Serilog with Elasticsearch sink.
    /// Provides production-ready defaults with environment-based customization.
    /// </summary>
    public class ElasticsearchSerilogConfiguration
    {
        private readonly string _serviceName;
        private readonly string _environment;
        private readonly string _version;
        private readonly ElasticsearchSinkOptions _elasticsearchOptions;

        public ElasticsearchSerilogConfiguration(
            string serviceName,
            string environment,
            string version = "1.0.0",
            string[] elasticsearchNodes = null)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _version = version;

            // Set Elasticsearch connection nodes
            var nodes = elasticsearchNodes ?? new[] { "http://elasticsearch:9200" };

            _elasticsearchOptions = new ElasticsearchSinkOptions(new Uri(nodes[0]))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
                IndexFormat = $"logs-{serviceName.ToLower()}-{environment.ToLower()}-{{0:yyyy.MM.dd}}",
                
                // Batch settings for performance
                BufferBaseFilename = null, // Set in development for local buffering
                BufferFileSizeLimitBytes = 52428800, // 50MB buffer
                BufferLogShippingInterval = TimeSpan.FromSeconds(30), // Ship logs every 30 seconds
                BufferRetainedInvalidPayloadsLimitBytes = 2097152, // 2MB for invalid payloads
                
                // Reliability settings
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                  EmitEventFailureHandling.RaiseCallback,
                FailureCallback = e => Console.WriteLine($"Unable to write {e.MessageTemplate} to Elasticsearch: {e.Exception}"),
                
                // Performance tuning
                NumberOfShards = 1,
                NumberOfReplicas = 0,
                
                // Connection pool settings
                DeadLetterIndexName = $"deadletters-{serviceName.ToLower()}",
                
                // Field mappings
                FieldNamesProvider = new DefaultFieldNameProvider(),
                
                // Minimum log level to send to Elasticsearch
                MinimumLogEventLevel = LogEventLevel.Information,
                
                // Connection settings
                ModifyConnectionSettings = settings =>
                {
                    settings.DisablePing(); // For Docker environments
                    settings.RequestTimeout(TimeSpan.FromSeconds(30));
                    return settings;
                }
            };

            // Set credentials if provided
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ELASTICSEARCH_USERNAME")))
            {
                var username = Environment.GetEnvironmentVariable("ELASTICSEARCH_USERNAME");
                var password = Environment.GetEnvironmentVariable("ELASTICSEARCH_PASSWORD");
                _elasticsearchOptions.ModifyConnectionSettings = settings =>
                {
                    settings.BasicAuthentication(username, password);
                    return settings;
                };
            }
        }

        /// <summary>
        /// Builds a complete Serilog logger configuration with Elasticsearch sink.
        /// </summary>
        public LoggerConfiguration BuildConfiguration(
            ICorrelationIdProvider correlationIdProvider = null,
            LogEventLevel minimumLevel = LogEventLevel.Information)
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.Elasticsearch(_elasticsearchOptions);

            // Add custom enrichers if correlation ID provider is available
            if (correlationIdProvider != null)
            {
                config = config.Enrich.With(
                    new LoggingEnricher(correlationIdProvider, _serviceName, _environment, _version));
            }

            // Add alternative Serilog sink for local file logging in Development
            if (_environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                config = config.WriteTo.File(
                    $"logs/{_serviceName.ToLower()}-{{Date}}.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {Level:u3} {Message:lj} {Properties:j}{NewLine}{Exception}");
            }

            return config;
        }

        /// <summary>
        /// Quick method to create and return a preconfigured logger.
        /// </summary>
        public ILogger CreateLogger(ICorrelationIdProvider correlationIdProvider = null)
        {
            return BuildConfiguration(correlationIdProvider).CreateLogger();
        }
    }

    /// <summary>
    /// Extension methods for IHostBuilder to simplify Serilog configuration.
    /// </summary>
    public static class SerilogHostBuilderExtensions
    {
        /// <summary>
        /// Adds Elasticsearch-backed Serilog logging to the host builder.
        /// </summary>
        public static WebApplicationBuilder AddElasticsearchLogging(
            this WebApplicationBuilder builder,
            string serviceName,
            string version = "1.0.0")
        {
            var environment = builder.Environment.EnvironmentName;
            var correlationIdProvider = new CorrelationIdProvider();

            var config = new ElasticsearchSerilogConfiguration(serviceName, environment, version);
            var logger = config.CreateLogger(correlationIdProvider);

            Log.Logger = logger;
            builder.Host.UseSerilog(logger);

            // Register correlation ID provider in DI
            builder.Services.AddSingleton<ICorrelationIdProvider>(correlationIdProvider);

            return builder;
        }
    }
}
