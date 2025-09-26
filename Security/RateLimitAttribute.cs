using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SmkcApi.Security
{
    /// <summary>
    /// Attribute to implement rate limiting on API endpoints
    /// </summary>
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private static readonly Dictionary<string, List<DateTime>> RequestHistory = new Dictionary<string, List<DateTime>>();
        private static readonly object LockObject = new object();

        /// <summary>
        /// Initialize rate limiting
        /// </summary>
        /// <param name="maxRequests">Maximum number of requests allowed</param>
        /// <param name="timeWindowMinutes">Time window in minutes</param>
        public RateLimitAttribute(int maxRequests = 100, int timeWindowMinutes = 1)
        {
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var clientIdentifier = GetClientIdentifier(actionContext.Request);
                
                if (string.IsNullOrEmpty(clientIdentifier))
                {
                    LogSecurityEvent("Unable to determine client identifier for rate limiting");
                    SetRateLimitResponse(actionContext, "Rate limiting validation failed");
                    return;
                }

                lock (LockObject)
                {
                    var currentTime = DateTime.UtcNow;
                    
                    // Initialize request history for new clients
                    if (!RequestHistory.ContainsKey(clientIdentifier))
                    {
                        RequestHistory[clientIdentifier] = new List<DateTime>();
                    }

                    var clientHistory = RequestHistory[clientIdentifier];
                    
                    // Remove old requests outside the time window
                    clientHistory.RemoveAll(requestTime => currentTime - requestTime > _timeWindow);
                    
                    // Check if client has exceeded rate limit
                    if (clientHistory.Count >= _maxRequests)
                    {
                        LogSecurityEvent($"Rate limit exceeded for client: {SecurityHelper.MaskSensitiveData(clientIdentifier)}");
                        SetRateLimitResponse(actionContext, "Rate limit exceeded", clientHistory.Count);
                        return;
                    }

                    // Add current request to history
                    clientHistory.Add(currentTime);
                    
                    LogSecurityEvent($"Rate limit check passed for client: {SecurityHelper.MaskSensitiveData(clientIdentifier)} " +
                                   $"(requests: {clientHistory.Count}/{_maxRequests})");
                }

                // Add rate limit headers to track usage
                AddRateLimitHeaders(actionContext.Request, clientIdentifier);

                base.OnActionExecuting(actionContext);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Rate limiting error: {ex.Message}");
                // Continue with request on rate limiting errors to avoid breaking the API
                base.OnActionExecuting(actionContext);
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                var clientIdentifier = GetClientIdentifier(actionExecutedContext.Request);
                
                if (!string.IsNullOrEmpty(clientIdentifier) && actionExecutedContext.Response != null)
                {
                    lock (LockObject)
                    {
                        if (RequestHistory.ContainsKey(clientIdentifier))
                        {
                            var clientHistory = RequestHistory[clientIdentifier];
                            var remainingRequests = Math.Max(0, _maxRequests - clientHistory.Count);
                            
                            // Add rate limit information to response headers
                            actionExecutedContext.Response.Headers.Add("X-RateLimit-Limit", _maxRequests.ToString());
                            actionExecutedContext.Response.Headers.Add("X-RateLimit-Remaining", remainingRequests.ToString());
                            actionExecutedContext.Response.Headers.Add("X-RateLimit-Reset", 
                                SecurityHelper.ToUnixTimeSeconds(DateTimeOffset.UtcNow.Add(_timeWindow)).ToString());
                            actionExecutedContext.Response.Headers.Add("X-RateLimit-Window", _timeWindow.TotalMinutes.ToString());
                        }
                    }
                }

                base.OnActionExecuted(actionExecutedContext);
            }
            catch (Exception ex)
            {
                LogSecurityEvent($"Rate limiting post-processing error: {ex.Message}");
                base.OnActionExecuted(actionExecutedContext);
            }
        }

        private string GetClientIdentifier(HttpRequestMessage request)
        {
            // Priority 1: Use API key if available (most reliable identifier)
            if (request.Properties.ContainsKey("ApiKey"))
            {
                return $"API_{request.Properties["ApiKey"]}";
            }

            // Priority 2: Use API key from headers
            if (request.Headers.Contains("X-API-Key"))
            {
                var apiKey = request.Headers.GetValues("X-API-Key").FirstOrDefault();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    return $"API_{apiKey}";
                }
            }

            // Priority 3: Use client IP address
            var clientIP = GetClientIPAddress(request);
            if (!string.IsNullOrEmpty(clientIP))
            {
                return $"IP_{clientIP}";
            }

            // Fallback: Use a combination of User-Agent and IP (less reliable)
            var userAgent = request.Headers.UserAgent?.ToString() ?? "Unknown";
            return $"UA_{userAgent.GetHashCode()}_{clientIP ?? "Unknown"}";
        }

        private string GetClientIPAddress(HttpRequestMessage request)
        {
            try
            {
                // Check stored client IP from IP whitelist attribute
                if (request.Properties.ContainsKey("ClientIP"))
                {
                    return request.Properties["ClientIP"].ToString();
                }

                // Check for forwarded IP headers
                if (request.Headers.Contains("X-Forwarded-For"))
                {
                    var forwardedFor = request.Headers.GetValues("X-Forwarded-For").FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        return forwardedFor.Split(',')[0].Trim();
                    }
                }

                if (request.Headers.Contains("X-Real-IP"))
                {
                    return request.Headers.GetValues("X-Real-IP").FirstOrDefault();
                }

                // Get IP from HTTP context
                if (request.Properties.ContainsKey("MS_HttpContext"))
                {
                    var httpContext = request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                    if (httpContext != null)
                    {
                        return SecurityHelper.GetClientIpAddress(httpContext.Request);
                    }
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        private void AddRateLimitHeaders(HttpRequestMessage request, string clientIdentifier)
        {
            // Headers will be added in OnActionExecuted method
            // This method is for future extensibility
        }

        private void SetRateLimitResponse(HttpActionContext context, string message, int currentRequests = 0)
        {
            var resetTime = SecurityHelper.ToUnixTimeSeconds(DateTimeOffset.UtcNow.Add(_timeWindow));
            
            var response = new
            {
                success = false,
                message = message,
                error = "RATE_LIMIT_EXCEEDED",
                retryAfter = (int)_timeWindow.TotalSeconds,
                limit = _maxRequests,
                current = currentRequests,
                resetTime = resetTime,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            context.Response = context.Request.CreateResponse((HttpStatusCode)429, response);
            
            // Add rate limit headers
            context.Response.Headers.Add("X-RateLimit-Limit", _maxRequests.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", "0");
            context.Response.Headers.Add("X-RateLimit-Reset", resetTime.ToString());
            context.Response.Headers.Add("Retry-After", ((int)_timeWindow.TotalSeconds).ToString());
            
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        }

        private void LogSecurityEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - RATE_LIMIT: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }

        /// <summary>
        /// Static method to clear rate limit history for a specific client (administrative use)
        /// </summary>
        public static void ClearClientHistory(string clientIdentifier)
        {
            lock (LockObject)
            {
                if (RequestHistory.ContainsKey(clientIdentifier))
                {
                    RequestHistory.Remove(clientIdentifier);
                    LogStaticSecurityEvent($"Rate limit history cleared for client: {SecurityHelper.MaskSensitiveData(clientIdentifier)}");
                }
            }
        }

        /// <summary>
        /// Static method to get current rate limit status for a client
        /// </summary>
        public static object GetClientRateStatus(string clientIdentifier)
        {
            lock (LockObject)
            {
                if (RequestHistory.ContainsKey(clientIdentifier))
                {
                    var clientHistory = RequestHistory[clientIdentifier];
                    var currentTime = DateTime.UtcNow;
                    
                    // Remove old requests
                    clientHistory.RemoveAll(requestTime => currentTime - requestTime > TimeSpan.FromMinutes(1));
                    
                    return new
                    {
                        ClientIdentifier = SecurityHelper.MaskSensitiveData(clientIdentifier),
                        CurrentRequests = clientHistory.Count,
                        RemainingRequests = Math.Max(0, 100 - clientHistory.Count), // Using default values
                        ResetTime = SecurityHelper.ToUnixTimeSeconds(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)))
                    };
                }
                
                return new
                {
                    ClientIdentifier = SecurityHelper.MaskSensitiveData(clientIdentifier),
                    CurrentRequests = 0,
                    RemainingRequests = 100,
                    ResetTime = SecurityHelper.ToUnixTimeSeconds(DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(1)))
                };
            }
        }

        private static void LogStaticSecurityEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - RATE_LIMIT_ADMIN: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }
    }
}