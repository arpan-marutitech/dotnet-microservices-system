// ============================================================================
// CORRELATION ID PROVIDER
// ============================================================================
// Manages correlation IDs for request tracing across microservices
// ============================================================================

using System;
using System.Collections.Generic;

namespace SharedLogging.Services
{
    /// <summary>
    /// Provides correlation ID for distributed tracing across microservices.
    /// Correlation IDs allow tracking a single request through multiple services.
    /// </summary>
    public interface ICorrelationIdProvider
    {
        /// <summary>Gets or generates the current correlation ID.</summary>
        string GetOrGenerateCorrelationId();

        /// <summary>Gets the current correlation ID or returns null if not set.</summary>
        string GetCorrelationId();

        /// <summary>Sets the correlation ID explicitly.</summary>
        void SetCorrelationId(string correlationId);

        /// <summary>Gets the request ID for the current HTTP request.</summary>
        string GetRequestId();

        /// <summary>Sets the request ID explicitly.</summary>
        void SetRequestId(string requestId);

        /// <summary>Generates a new correlation ID.</summary>
        string GenerateCorrelationId();
    }

    /// <summary>
    /// Default implementation of correlation ID provider using AsyncLocal for thread-safe storage.
    /// </summary>
    public class CorrelationIdProvider : ICorrelationIdProvider
    {
        private static readonly AsyncLocal<string> CorrelationIdValue = new();
        private static readonly AsyncLocal<string> RequestIdValue = new();

        public string GetOrGenerateCorrelationId()
        {
            if (string.IsNullOrEmpty(CorrelationIdValue.Value))
            {
                CorrelationIdValue.Value = GenerateCorrelationId();
            }
            return CorrelationIdValue.Value;
        }

        public string GetCorrelationId()
        {
            return CorrelationIdValue.Value;
        }

        public void SetCorrelationId(string correlationId)
        {
            CorrelationIdValue.Value = correlationId;
        }

        public string GetRequestId()
        {
            return RequestIdValue.Value ??= GenerateCorrelationId();
        }

        public void SetRequestId(string requestId)
        {
            RequestIdValue.Value = requestId;
        }

        public string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
