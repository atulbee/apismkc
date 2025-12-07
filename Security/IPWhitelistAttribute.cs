using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Reflection; // added for reflection

namespace SmkcApi.Security
{
    /// <summary>
    /// Attribute to restrict API access to whitelisted IP addresses
    /// </summary>
    public class IPWhitelistAttribute : ActionFilterAttribute
    {
        private static readonly HashSet<string> WhitelistedIPs = new HashSet<string>
        {
            // Add bank's IP addresses here
            // For development/testing - remove in production
            "127.0.0.1",           // Local development
            "::1",                 // IPv6 localhost
            "192.168.1.0/24",      // Local network range (example)
            "192.168.40.",
            // Example bank IP addresses - replace with actual bank IPs
            "203.0.113.10",        // Bank's primary IP
            "203.0.113.11",        // Bank's secondary IP
            "203.0.113.0/24"       // Bank's IP range
        };

        // IP whitelist disabled per request: allow all IPs
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                // Capture client IP for logging but do not enforce whitelist
                var clientIp = GetClientIPAddress(actionContext.Request);
                if (!string.IsNullOrEmpty(clientIp))
                {
                    actionContext.Request.Properties["ClientIP"] = clientIp;
                    LogSecurityEvent($"IP whitelist disabled. Allowing IP: {clientIp}");
                }

                // Proceed without restriction
                base.OnActionExecuting(actionContext);
            }
            catch (Exception ex)
            {
                // Do not block requests if IP extraction fails
                LogSecurityEvent($"IP whitelist disabled. Error extracting IP: {ex.Message}");
                base.OnActionExecuting(actionContext);
            }
        }

        private string GetClientIPAddress(HttpRequestMessage request)
        {
            // Check for forwarded IP first (load balancer/proxy scenarios)
            if (request.Headers.Contains("X-Forwarded-For"))
            {
                var forwardedFor = request.Headers.GetValues("X-Forwarded-For").FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    // Get the first IP if multiple IPs are present
                    var firstIP = forwardedFor.Split(',')[0].Trim();
                    if (IsValidIPAddress(firstIP))
                        return firstIP;
                }
            }

            // Check for real IP header
            if (request.Headers.Contains("X-Real-IP"))
            {
                var realIP = request.Headers.GetValues("X-Real-IP").FirstOrDefault();
                if (!string.IsNullOrEmpty(realIP) && IsValidIPAddress(realIP))
                    return realIP;
            }

            // Get IP from request properties (set by IIS/hosting environment)
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                var httpContext = request.Properties["MS_HttpContext"] as System.Web.HttpContextWrapper;
                if (httpContext != null)
                {
                    return SecurityHelper.GetClientIpAddress(httpContext.Request);
                }
            }

            // Fallback to OWIN context if available
            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                var owinContext = request.Properties["MS_OwinContext"]; // unknown type at compile time
                var remoteIp = TryGetRemoteIpFromOwin(owinContext);
                if (IsValidIPAddress(remoteIp))
                    return remoteIp;
            }

            return null;
        }

        private string TryGetRemoteIpFromOwin(object owinContext)
        {
            try
            {
                if (owinContext == null) return null;
                // Access owinContext.Request.RemoteIpAddress via reflection
                var requestProp = owinContext.GetType().GetProperty("Request", BindingFlags.Public | BindingFlags.Instance);
                if (requestProp == null) return null;
                var requestObj = requestProp.GetValue(owinContext, null);
                if (requestObj == null) return null;
                var remoteIpProp = requestObj.GetType().GetProperty("RemoteIpAddress", BindingFlags.Public | BindingFlags.Instance);
                var value = remoteIpProp?.GetValue(requestObj, null) as string;
                return value;
            }
            catch
            {
                return null;
            }
        }

        private bool IsIPWhitelisted(string clientIP)
        {
            if (string.IsNullOrEmpty(clientIP))
                return false;

            // Check exact IP matches
            if (WhitelistedIPs.Contains(clientIP))
                return true;

            // Check CIDR ranges
            foreach (var whitelistedEntry in WhitelistedIPs.Where(ip => ip.Contains("/")))
            {
                if (IsIPInCIDRRange(clientIP, whitelistedEntry))
                    return true;
            }

            return false;
        }

        private bool IsIPInCIDRRange(string ipAddress, string cidrRange)
        {
            try
            {
                var parts = cidrRange.Split('/');
                if (parts.Length != 2)
                    return false;

                var networkAddress = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);
                var clientAddress = IPAddress.Parse(ipAddress);

                // Convert to bytes for comparison
                var networkBytes = networkAddress.GetAddressBytes();
                var clientBytes = clientAddress.GetAddressBytes();

                if (networkBytes.Length != clientBytes.Length)
                    return false;

                // Calculate number of bytes and bits to check
                var bytesToCheck = prefixLength / 8;
                var bitsToCheck = prefixLength % 8;

                // Check full bytes
                for (int i = 0; i < bytesToCheck; i++)
                {
                    if (networkBytes[i] != clientBytes[i])
                        return false;
                }

                // Check remaining bits if any
                if (bitsToCheck > 0 && bytesToCheck < networkBytes.Length)
                {
                    var mask = (byte)(0xFF << (8 - bitsToCheck));
                    if ((networkBytes[bytesToCheck] & mask) != (clientBytes[bytesToCheck] & mask))
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SetForbiddenResponse(HttpActionContext context, string message)
        {
            var response = new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                requestId = Guid.NewGuid().ToString()
            };

            context.Response = context.Request.CreateResponse(HttpStatusCode.Forbidden, response);

            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        }

        private void LogSecurityEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - IP_WHITELIST: {message}";
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

        /// <summary>
        /// Static method to add IP to whitelist (for administrative purposes)
        /// </summary>
        public static void AddIPToWhitelist(string ipAddress)
        {
            if (IsValidIPAddress(ipAddress) || ipAddress.Contains("/"))
            {
                WhitelistedIPs.Add(ipAddress);
                LogStaticSecurityEvent($"IP added to whitelist: {ipAddress}");
            }
        }

        /// <summary>
        /// Static method to remove IP from whitelist (for administrative purposes)
        /// </summary>
        public static void RemoveIPFromWhitelist(string ipAddress)
        {
            if (WhitelistedIPs.Remove(ipAddress))
            {
                LogStaticSecurityEvent($"IP removed from whitelist: {ipAddress}");
            }
        }

        private static bool IsValidIPAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }

        private static void LogStaticSecurityEvent(string message)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - IP_WHITELIST_ADMIN: {message}";
            System.Diagnostics.Trace.TraceInformation(logEntry);
        }
    }
}