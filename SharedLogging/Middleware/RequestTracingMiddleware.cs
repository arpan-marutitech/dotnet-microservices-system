// ============================================================================
// REQUEST TRACING MIDDLEWARE
// ============================================================================
// Captures request/response information for comprehensive HTTP tracing
// and correlates requests across microservices.
// ============================================================================

using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SharedLogging.Services;

namespace SharedLogging.Middleware
{
    /// <summary>
    /// Middleware that captures and logs detailed HTTP request/response information.
    /// Includes automatic correlation ID handling and performance metrics.
    /// </summary>
    public class RequestTracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private const string CorrelationIdHeader = "X-Correlation-Id";
        private const string RequestIdHeader = "X-Request-Id";

        public RequestTracingMiddleware(
            RequestDelegate next,
            ILogger logger,
            ICorrelationIdProvider correlationIdProvider)
        {
            _next = next;
            _logger = logger;
            _correlationIdProvider = correlationIdProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for health check endpoints
            if (context.Request.Path.Value?.Equals("/health", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _next(context);
                return;
            }

            // Extract or generate correlation ID from headers
            var correlationId = context.Request.Headers.ContainsKey(CorrelationIdHeader)
                ? context.Request.Headers[CorrelationIdHeader].ToString()
                : _correlationIdProvider.GenerateCorrelationId();

            // Extract or generate request ID
            var requestId = context.Request.Headers.ContainsKey(RequestIdHeader)
                ? context.Request.Headers[RequestIdHeader].ToString()
                : Guid.NewGuid().ToString("N");

            // Set in provider for use throughout the request
            _correlationIdProvider.SetCorrelationId(correlationId);
            _correlationIdProvider.SetRequestId(requestId);

            // Add to response headers for debugging
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            context.Response.Headers[RequestIdHeader] = requestId;

            // Push correlation IDs to logging context
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestId", requestId))
            {
                var stopwatch = Stopwatch.StartNew();

                // Log request details
                await LogRequest(context);

                // Capture original response body
                var originalBodyStream = context.Response.Body;

                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        context.Response.Body = memoryStream;

                        // Call next middleware
                        await _next(context);

                        stopwatch.Stop();

                        // Log response details
                        await LogResponse(context, stopwatch.ElapsedMilliseconds);

                        // Copy from memory to original
                        await memoryStream.CopyToAsync(originalBodyStream);
                    }
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }

        private async Task LogRequest(HttpContext context)
        {
            var request = context.Request;

            // Read the body
            var bodyContent = ""; // Empty for GET requests
            if (request.ContentLength > 0 && request.Body.CanRead)
            {
                request.Body.Position = 0;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    bodyContent = await reader.ReadToEndAsync();
                    request.Body.Position = 0; // Reset for next middleware
                }

                // Truncate large bodies
                if (bodyContent.Length > 2000)
                {
                    bodyContent = bodyContent.Substring(0, 2000) + "... [truncated]";
                }
            }

            // Get user information if authenticated
            var userId = context.User?.FindFirst("sub")?.Value ??
                        context.User?.FindFirst("nameid")?.Value ??
                        context.User?.Identity?.Name ?? "Anonymous";

            var properties = new
            {
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                ContentType = request.ContentType,
                ContentLength = request.ContentLength,
                Body = bodyContent,
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserId = userId,
                UserAgent = request.Headers["User-Agent"].ToString()
            };

            _logger.Information(
                "HTTP Request: {Method} {Path}",
                request.Method,
                request.Path);
        }

        private async Task LogResponse(HttpContext context, long elapsedMs)
        {
            var response = context.Response;

            // Read response body
            string bodyContent = "";
            if (response.Body is MemoryStream bodyAsMemory)
            {
                bodyContent = Encoding.UTF8.GetString(bodyAsMemory.ToArray());

                // Truncate large bodies
                if (bodyContent.Length > 2000)
                {
                    bodyContent = bodyContent.Substring(0, 2000) + "... [truncated]";
                }
            }

            var logLevel = response.StatusCode >= 500
                ? LogEventLevel.Error
                : response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;

            var logMessage = $"HTTP Response: {response.StatusCode} in {elapsedMs}ms";

            var properties = new
            {
                StatusCode = response.StatusCode,
                DurationMs = elapsedMs,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                Body = bodyContent
            };

            if (logLevel == LogEventLevel.Error)
            {
                _logger.Error(logMessage);
            }
            else if (logLevel == LogEventLevel.Warning)
            {
                _logger.Warning(logMessage);
            }
            else
            {
                _logger.Information(logMessage);
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension method to register the request tracing middleware.
    /// </summary>
    public static class RequestTracingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestTracing(
            this IApplicationBuilder app,
            ICorrelationIdProvider correlationIdProvider = null)
        {
            return app.UseMiddleware<RequestTracingMiddleware>(correlationIdProvider);
        }
    }
}
