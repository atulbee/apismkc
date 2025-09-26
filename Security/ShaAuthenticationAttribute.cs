using System;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http;
using System.Net;

namespace SmkcApi.Security
{
    /// <summary>
    /// Attribute to enforce SHA-based authentication on API endpoints
    /// </summary>
    public class ShaAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                // Check if request has been authenticated by the message handler
                if (!actionContext.Request.Properties.ContainsKey("ApiKey"))
                {
                    SetUnauthorizedResponse(actionContext, "Request not properly authenticated");
                    return;
                }

                var apiKey = actionContext.Request.Properties["ApiKey"]?.ToString();
                var authenticatedAt = actionContext.Request.Properties["AuthenticatedAt"] as DateTime?;

                // Additional validation: ensure authentication is recent (within last 5 minutes)
                if (authenticatedAt.HasValue)
                {
                    var timeSinceAuth = DateTime.UtcNow - authenticatedAt.Value;
                    if (timeSinceAuth.TotalMinutes > 5)
                    {
                        LogSecurityEvent($"Stale authentication detected for API Key: {SecurityHelper.MaskSensitiveData(apiKey)}");
                        SetUnauthorizedResponse(actionContext, "Authentication expired");
                        return;
                    }
                }

                // Add request tracking for audit purposes
                var requestId = Guid.NewGuid().ToString();
                actionContext.Request.Properties["RequestId"] = requestId;

                LogSecurityEvent($"API access granted - Key: {SecurityHelper.MaskSensitiveData(apiKey)}, " +
                               $"Endpoint: {actionContext.Request.Method} {actionContext.Request.RequestUri.PathAndQuery}, " +
                               $"RequestId: {requestId}");

                base.OnActionExecuting(actionContext);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"SHA Authentication filter error: {ex.Message}");
                SetUnauthorizedResponse(actionContext, "Authentication validation failed");
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                var apiKey = actionExecutedContext.Request.Properties.ContainsKey("ApiKey") ? 
                           actionExecutedContext.Request.Properties["ApiKey"]?.ToString() : "Unknown";
                var requestId = actionExecutedContext.Request.Properties.ContainsKey("RequestId") ? 
                              actionExecutedContext.Request.Properties["RequestId"]?.ToString() : "Unknown";

                var statusCode = actionExecutedContext.Response?.StatusCode ?? HttpStatusCode.InternalServerError;

                LogSecurityEvent($"API request completed - Key: {SecurityHelper.MaskSensitiveData(apiKey)}, " +
                               $"Status: {(int)statusCode} {statusCode}, " +
                               $"RequestId: {requestId}");

                // Add security headers to response
                if (actionExecutedContext.Response != null)
                {
                    actionExecutedContext.Response.Headers.Add("X-Request-ID", requestId);
                    actionExecutedContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    actionExecutedContext.Response.Headers.Add("X-Frame-Options", "DENY");
                    actionExecutedContext.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                    actionExecutedContext.Response.Headers.Add("Cache-Control", "no-store, no-cache, must-revalidate");
                    actionExecutedContext.Response.Headers.Add("Pragma", "no-cache");
                }

                base.OnActionExecuted(actionExecutedContext);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"SHA Authentication post-processing error: {ex.Message}");
            }
        }

        private void SetUnauthorizedResponse(HttpActionContext context, string message)
        {
            var response = new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                requestId = Guid.NewGuid().ToString()
            };

            context.Response = context.Request.CreateResponse(HttpStatusCode.Unauthorized, response);
            
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        }

        private void LogSecurityEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - SHA_AUTH: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
            
            // Optional: Log to Windows Event Log for security monitoring
            try
            {
                System.Diagnostics.EventLog.WriteEntry("SMKC API", logEntry, System.Diagnostics.EventLogEntryType.Information);
            }
            catch
            {
                // Ignore event log failures to prevent breaking the API
            }
        }
    }
}