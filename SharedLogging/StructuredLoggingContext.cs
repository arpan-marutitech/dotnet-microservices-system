// ============================================================================
// STRUCTURED LOGGING CONTEXT
// ============================================================================
// Provides helper methods for structured logging with consistent properties.
// ============================================================================

using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SharedLogging.Utilities
{
    /// <summary>
    /// Helper class for structured logging with contextual information.
    /// Provides convenience methods and scoped contexts for logging.
    /// </summary>
    public static class StructuredLoggingContext
    {
        /// <summary>
        /// Sets up a scope for logging a specific operation with performance tracking.
        /// </summary>
        public static IDisposable LogOperation(string operationName, string operationId = null)
        {
            operationId ??= Guid.NewGuid().ToString("N");
            var stopwatch = Stopwatch.StartNew();

            var properties = new Dictionary<string, object>
            {
                { "OperationName", operationName },
                { "OperationId", operationId },
                { "OperationStartTime", DateTime.UtcNow }
            };

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Sets up a scope for logging a database operation.
        /// </summary>
        public static IDisposable LogDatabaseOperation(
            string operationName,
            string query = null,
            Dictionary<string, object> parameters = null)
        {
            var properties = new Dictionary<string, object>
            {
                { "DbOperation", operationName },
                { "DbOperationStartTime", DateTime.UtcNow }
            };

            if (!string.IsNullOrEmpty(query))
            {
                properties["DbQuery"] = query;
            }

            if (parameters?.Any() == true)
            {
                properties["DbParameters"] = parameters;
            }

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Sets up a scope for logging an API call to an external service.
        /// </summary>
        public static IDisposable LogExternalApiCall(
            string serviceName,
            string endpoint,
            string httpMethod = "GET")
        {
            var properties = new Dictionary<string, object>
            {
                { "ExternalService", serviceName },
                { "ExternalEndpoint", endpoint },
                { "ExternalHttpMethod", httpMethod },
                { "ExternalCallStartTime", DateTime.UtcNow }
            };

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Sets up a scope for logging user-related operations.
        /// </summary>
        public static IDisposable LogUserOperation(
            string userId,
            string userEmail = null,
            Dictionary<string, object> additionalInfo = null)
        {
            var properties = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "UserOperationStartTime", DateTime.UtcNow }
            };

            if (!string.IsNullOrEmpty(userEmail))
            {
                properties["UserEmail"] = userEmail;
            }

            if (additionalInfo?.Any() == true)
            {
                foreach (var kvp in additionalInfo)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Sets up a scope for logging cache operations.
        /// </summary>
        public static IDisposable LogCacheOperation(
            string operationType, // "Get", "Set", "Remove", "Clear"
            string cacheKey,
            string cacheRegion = null)
        {
            var properties = new Dictionary<string, object>
            {
                { "CacheOperation", operationType },
                { "CacheKey", cacheKey },
                { "CacheOperationStartTime", DateTime.UtcNow }
            };

            if (!string.IsNullOrEmpty(cacheRegion))
            {
                properties["CacheRegion"] = cacheRegion;
            }

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Sets up a scope for logging messaging/queue operations.
        /// </summary>
        public static IDisposable LogMessagingOperation(
            string messageType,
            string queueName,
            string messageId = null)
        {
            var properties = new Dictionary<string, object>
            {
                { "MessageType", messageType },
                { "QueueName", queueName },
                { "MessagingOperationStartTime", DateTime.UtcNow }
            };

            if (!string.IsNullOrEmpty(messageId))
            {
                properties["MessageId"] = messageId;
            }

            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Pushes custom properties into the logging context.
        /// </summary>
        public static IDisposable PushContext(Dictionary<string, object> properties)
        {
            return LogContext.PushProperties(properties);
        }

        /// <summary>
        /// Logs an exception with rich context information.
        /// </summary>
        public static void LogException(
            Exception exception,
            string context,
            ILogger logger,
            Dictionary<string, object> additionalProperties = null)
        {
            var properties = new Dictionary<string, object>
            {
                { "ExceptionContext", context },
                { "ExceptionType", exception.GetType().Name },
                { "ExceptionStackTrace", exception.StackTrace }
            };

            if (additionalProperties?.Any() == true)
            {
                foreach (var kvp in additionalProperties)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            using (LogContext.PushProperties(properties))
            {
                logger.Error(exception, "An error occurred in {Context}", context);
            }
        }

        /// <summary>
        /// Measures and logs the execution time of an operation.
        /// </summary>
        public static void LogExecutionTime(
            string operationName,
            long durationMs,
            ILogger logger,
            LogLevel level = LogLevel.Information)
        {
            var message = $"Operation {operationName} completed in {durationMs}ms";

            if (durationMs > 5000) // More than 5 seconds
            {
                logger.Warning("{Message} - This operation may need optimization", message);
            }
            else
            {
                logger.Information("{Message}", message);
            }
        }
    }

    /// <summary>
    /// Disposable scope for automatic performance/exception logging.
    /// </summary>
    public class LoggingScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly IDisposable _logContext;
        private bool _disposed;

        public LoggingScope(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _logContext = StructuredLoggingContext.LogOperation(operationName);

            _logger.Information("Starting operation: {OperationName}", operationName);
        }

        public void LogSuccess(string message = null)
        {
            _stopwatch.Stop();
            message ??= $"Operation {_operationName} completed successfully";
            _logger.Information("{Message} in {Duration}ms", message, _stopwatch.ElapsedMilliseconds);
        }

        public void LogWarning(string message)
        {
            _stopwatch.Stop();
            _logger.Warning("{Message} after {Duration}ms", message, _stopwatch.ElapsedMilliseconds);
        }

        public void LogError(string message, Exception ex = null)
        {
            _stopwatch.Stop();
            if (ex != null)
            {
                _logger.Error(ex, "{Message} after {Duration}ms", message, _stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Error("{Message} after {Duration}ms", message, _stopwatch.ElapsedMilliseconds);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _stopwatch.Stop();
            if (_stopwatch.ElapsedMilliseconds > 10000)
            {
                _logger.Warning("Operation {OperationName} took longer than expected: {Duration}ms",
                    _operationName, _stopwatch.ElapsedMilliseconds);
            }

            _logContext?.Dispose();
            _disposed = true;
        }
    }
}
