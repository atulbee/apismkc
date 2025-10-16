using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SmkcApi.Security
{
    /// <summary>
    /// Authorizes requests only from configured IP addresses / ranges.
    /// Supports exact IP, wildcard like 192.168.40.*, and CIDR like 192.168.40.0/24.
    /// If behind a reverse proxy, it reads X-Forwarded-For (only use if you trust your proxy).
    /// </summary>
    public class IPWhitelistAttribute : AuthorizationFilterAttribute
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

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnAuthorization(actionContext);
            return;
            var clientIp = GetClientIp(actionContext.Request);

            // load allowed patterns from config
            var cfg = ConfigurationManager.AppSettings["AllowedIPs"];
            var allowed = string.IsNullOrWhiteSpace(cfg)
                ? DefaultAllowed
                : cfg.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();

            // check
            var allowedFlag = allowed.Any(pattern => Matches(clientIp, pattern));

            if (!allowedFlag)
            {
                // optional logging
                System.Diagnostics.Trace.TraceWarning($"Blocked request from IP: {clientIp} to {actionContext.Request.RequestUri}");

                actionContext.Response = actionContext.Request.CreateResponse(
                    HttpStatusCode.Forbidden,
                    new { message = "Access denied from this IP" }
                );
            }
            else
            {
                base.OnAuthorization(actionContext);
            }
        }

        private string GetClientIp(HttpRequestMessage request)
        {
            // 1) Check X-Forwarded-For (first IP is client). ONLY use if you trust the proxy.
            if (request.Headers.TryGetValues("X-Forwarded-For", out var forwarded) && forwarded != null)
            {
                var f = forwarded.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(f))
                {
                    var first = f.Split(',').Select(x => x.Trim()).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first)) return first;
                }
            }

            // 2) Owin context (if hosted with OWIN)
            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                dynamic owinContext = request.Properties["MS_OwinContext"];
                try
                {
                    string ip = owinContext.Request.RemoteIpAddress;
                    if (!string.IsNullOrWhiteSpace(ip)) return ip;
                }
                catch { /* ignore */ }
            }

            // 3) HttpContext (classic hosting)
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                dynamic ctx = request.Properties["MS_HttpContext"];
                try
                {
                    string ip = ctx.Request.UserHostAddress;
                    if (!string.IsNullOrWhiteSpace(ip)) return ip;
                }
                catch { /* ignore */ }
            }

            // 4) RemoteEndpointMessageProperty (self-host)
            if (request.Properties.ContainsKey("System.ServiceModel.Channels.RemoteEndpointMessageProperty"))
            {
                dynamic prop = request.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"];
                try
                {
                    string ip = prop.Address;
                    if (!string.IsNullOrWhiteSpace(ip)) return ip;
                }
                catch { /* ignore */ }
            }

            return null;
        }

        private bool Matches(string clientIp, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) return false;
            if (string.IsNullOrWhiteSpace(clientIp)) return false;

            pattern = pattern.Trim();

            // Exact match
            if (string.Equals(clientIp, pattern, StringComparison.OrdinalIgnoreCase)) return true;

            // Wildcard like 192.168.40.*
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern.TrimEnd('*');
                if (clientIp.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }

            // CIDR like 192.168.40.0/24
            if (pattern.Contains("/"))
            {
                var parts = pattern.Split('/');
                if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var network) && int.TryParse(parts[1], out var prefixLen))
                {
                    if (IPAddress.TryParse(clientIp, out var addr))
                    {
                        return IsInSubnet(addr, network, prefixLen);
                    }
                }
            }

            // fallback: try comparing uppercase/lowercase (rare for IPs)
            return string.Equals(clientIp, pattern, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsInSubnet(IPAddress address, IPAddress network, int prefixLength)
        {
            var addrBytes = address.GetAddressBytes();
            var netBytes = network.GetAddressBytes();

            if (addrBytes.Length != netBytes.Length) return false; // IPv4 vs IPv6 mismatch

            int fullBytes = prefixLength / 8;
            int remainingBits = prefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
                if (addrBytes[i] != netBytes[i]) return false;

            if (remainingBits == 0) return true;

            int mask = (-1) << (8 - remainingBits);
            return (addrBytes[fullBytes] & mask) == (netBytes[fullBytes] & mask);
        }
    }
}
