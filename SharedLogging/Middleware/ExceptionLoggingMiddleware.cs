// ============================================================================
// EXCEPTION LOGGING MIDDLEWARE
// ============================================================================
// Captures and logs exceptions with rich context and correlation tracking.
// ============================================================================

using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SharedLogging.Middleware
{
    /// <summary>
    /// Global exception handling middleware that logs all unhandled exceptions
    /// and returns consistent error responses.
    /// </summary>
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var logger = context.RequestServices.GetService(typeof(ILogger)) as ILogger;

            var response = context.Response;
            response.ContentType = "application/json";

            var exceptionType = exception.GetType().Name;
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            var requestId = context.Items["RequestId"]?.ToString() ?? "N/A";

            // Log exception with structured properties
            using (LogContext.PushProperty("ExceptionType", exceptionType))
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                logger?.Error(exception, "An unhandled exception occurred: {ExceptionMessage}", exception.Message);
            }

            // Determine status code based on exception type
            var statusCode = exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            response.StatusCode = (int)statusCode;

            var errorResponse = new
            {
                message = "An error occurred processing your request",
                exceptionType = exceptionType,
                correlationId = correlationId,
                requestId = requestId,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            };

            return response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    /// <summary>
    /// Extension method to register exception logging middleware.
    /// </summary>
    public static class ExceptionLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionLoggingMiddleware>();
        }
    }
}
