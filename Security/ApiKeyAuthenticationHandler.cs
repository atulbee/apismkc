using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Configuration;
using System.Collections.Generic;

namespace SmkcApi.Security
{
    public class ApiKeyAuthenticationHandler : DelegatingHandler
    {
        private static readonly HashSet<string> ValidApiKeys = new HashSet<string>
        {
            // Production API Keys
            "BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5",
            "BANK002_9c6f3a8e1d4b7c0f2e5a8b1c4d7f0a3e6b9c2d5f8a1b4c7e0d3f6a9b2c5d8e1f4",
            
            // Test API Keys for localhost development
            "TEST_API_KEY_12345678901234567890123456789012",
            "DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890",
            "ADMIN_API_KEY_XYZ12345678901234567890ABC456"
        };

        private static readonly Dictionary<string, string> ApiKeySecrets = new Dictionary<string, string>
        {
            // Production secrets
            ["BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5"] = 
                "S3cur3K3y!B4nk001#2024$Pr0d&V3ryL0ngS3cr3tK3yF0rB4nk1ng",
            ["BANK002_9c6f3a8e1d4b7c0f2e5a8b1c4d7f0a3e6b9c2d5f8a1b4c7e0d3f6a9b2c5d8e1f4"] = 
                "An0th3rS3cur3K3y!B4nk002#2024$V3ryS3cr3tK3yF0rB4nk2ng",
            
            // Test secrets for localhost development
            ["TEST_API_KEY_12345678901234567890123456789012"] = 
                "TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890",
            ["DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890"] = 
                "DEV_SECRET_KEY_FGHIJ67890KLMNO12345PQRST",
            ["ADMIN_API_KEY_XYZ12345678901234567890ABC456"] = 
                "ADMIN_SECRET_KEY_ABC45678901234567890DEF"
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                // Skip authentication for OPTIONS requests (CORS preflight)
                if (request.Method == HttpMethod.Options)
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                // Extract authentication headers
                if (!request.Headers.Contains("X-API-Key"))
                {
                    return CreateUnauthorizedResponse("Missing API Key");
                }

                if (!request.Headers.Contains("X-Timestamp"))
                {
                    return CreateUnauthorizedResponse("Missing Timestamp");
                }

                if (!request.Headers.Contains("X-Signature"))
                {
                    return CreateUnauthorizedResponse("Missing Signature");
                }

                var apiKey = request.Headers.GetValues("X-API-Key").FirstOrDefault();
                var timestamp = request.Headers.GetValues("X-Timestamp").FirstOrDefault();
                var signature = request.Headers.GetValues("X-Signature").FirstOrDefault();

                // Validate API key format
                if (!SecurityHelper.IsValidApiKeyFormat(apiKey))
                {
                    return CreateUnauthorizedResponse("Invalid API Key format");
                }

                // Validate API key exists
                if (!ValidApiKeys.Contains(apiKey))
                {
                    LogSecurityEvent($"Invalid API Key attempted: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    return CreateUnauthorizedResponse("Invalid API Key");
                }

                // Validate timestamp
                if (!SecurityHelper.IsValidTimestamp(timestamp))
                {
                    LogSecurityEvent($"Invalid timestamp from API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    return CreateUnauthorizedResponse("Invalid or expired timestamp");
                }

                // Get request body for signature validation
                string requestBody = "";
                if (request.Content != null)
                {
                    requestBody = await request.Content.ReadAsStringAsync();
                }

                // Validate signature
                var secretKey = ApiKeySecrets[apiKey];
                var expectedSignature = SecurityHelper.CreateRequestSignature(
                    request.Method.Method,
                    request.RequestUri.PathAndQuery,
                    requestBody,
                    timestamp,
                    apiKey,
                    secretKey
                );

                if (signature != expectedSignature)
                {
                    LogSecurityEvent($"Invalid signature from API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                    return CreateUnauthorizedResponse("Invalid signature");
                }

                // Add authenticated user information to request properties
                request.Properties["ApiKey"] = apiKey;
                request.Properties["AuthenticatedAt"] = DateTime.UtcNow;

                LogSecurityEvent($"Successful authentication for API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Authentication error: {ex.Message}");
                return CreateUnauthorizedResponse("Authentication failed");
            }
        }

        private HttpResponseMessage CreateUnauthorizedResponse(string message)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new StringContent($"{{\"success\":false,\"message\":\"{message}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\"}}")
            };
            
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            // Add security headers
            response.Headers.Add("X-Content-Type-Options", "nosniff");
            response.Headers.Add("X-Frame-Options", "DENY");
            response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            return response;
        }

        private void LogSecurityEvent(string message)
        {
            // In production, use a proper logging framework like NLog, Serilog, etc.
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SECURITY: {message}";
            System.Diagnostics.Trace.TraceWarning(logEntry);
            
            // Optional: Log to Windows Event Log for security monitoring
            try
            {
                System.Diagnostics.EventLog.WriteEntry("SMKC API", logEntry, System.Diagnostics.EventLogEntryType.Warning);
            }
            catch
            {
                // Ignore event log failures to prevent breaking the API
            }
        }
    }
}