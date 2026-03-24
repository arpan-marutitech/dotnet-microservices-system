// ============================================================================
// LOGGING ENRICHER
// ============================================================================
// Enriches logs with contextual information like service name, correlation ID, etc.
// ============================================================================

using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace SharedLogging.Services
{
    /// <summary>
    /// Serilog enricher that adds contextual information to all log events.
    /// This ensures consistent structured logging across all microservices.
    /// </summary>
    public class LoggingEnricher : ILogEventEnricher
    {
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly string _serviceName;
        private readonly string _environment;
        private readonly string _version;

        public LoggingEnricher(
            ICorrelationIdProvider correlationIdProvider,
            string serviceName,
            string environment,
            string version = "1.0.0")
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _version = version;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Add service name for filtering by service
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "Service", _serviceName));

            // Add environment for distinguishing Dev/Staging/Prod logs
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "Environment", _environment));

            // Add version for debugging version-specific issues
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "Version", _version));

            // Add correlation ID for distributed tracing
            var correlationId = _correlationIdProvider.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "CorrelationId", correlationId));
            }

            // Add request ID
            var requestId = _correlationIdProvider.GetRequestId();
            if (!string.IsNullOrEmpty(requestId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "RequestId", requestId));
            }

            // Add machine name for infrastructure diagnostics
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "MachineName", Environment.MachineName));

            // Add timestamp in UTC (redundant but helps with certain tools)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "UtcTimestamp", DateTime.UtcNow));

            // Add process ID for container diagnostics
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "ProcessId", System.Diagnostics.Process.GetCurrentProcess().Id));
        }
    }

    /// <summary>
    /// Enricher for adding HTTP request context to logs.
    /// </summary>
    public class HttpContextEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return;

            // Add HTTP method
            if (!string.IsNullOrEmpty(httpContext.Request.Method))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "HttpMethod", httpContext.Request.Method));
            }

            // Add request path
            if (!string.IsNullOrEmpty(httpContext.Request.Path))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "RequestPath", httpContext.Request.Path.Value));
            }

            // Add query string
            if (httpContext.Request.QueryString.HasValue)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "QueryString", httpContext.Request.QueryString.Value));
            }

            // Add client IP
            var remoteIpAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIpAddress))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ClientIp", remoteIpAddress));
            }

            // Add User ID if authenticated
            var userId = httpContext.User?.FindFirst("sub")?.Value ??
                        httpContext.User?.FindFirst("nameid")?.Value ??
                        httpContext.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "UserId", userId));
            }
        }
    }

    /// <summary>
    /// Enricher for adding custom application context.
    /// </summary>
    public class ApplicationContextEnricher : ILogEventEnricher
    {
        private readonly Dictionary<string, object> _contextData;

        public ApplicationContextEnricher(Dictionary<string, object> contextData = null)
        {
            _contextData = contextData ?? new Dictionary<string, object>();
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var kvp in _contextData)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(kvp.Key, kvp.Value));
            }
        }

        /// <summary>Add or update context data.</summary>
        public void AddOrUpdate(string key, object value)
        {
            _contextData[key] = value;
        }
    }
}
