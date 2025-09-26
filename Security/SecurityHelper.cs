using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SmkcApi.Security
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Generates SHA-256 hash of the input string
        /// </summary>
        public static string GenerateSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Creates request signature using SHA-256 HMAC
        /// </summary>
        public static string CreateRequestSignature(string httpMethod, string requestUri, string requestBody, string timestamp, string apiKey, string secretKey)
        {
            // Create string to sign: HTTP_METHOD + REQUEST_URI + REQUEST_BODY + TIMESTAMP + API_KEY
            var stringToSign = $"{httpMethod.ToUpper()}{requestUri}{requestBody ?? ""}{timestamp}{apiKey}";
            
            return CreateHmacSha256Signature(stringToSign, secretKey);
        }

        /// <summary>
        /// Creates HMAC-SHA256 signature
        /// </summary>
        public static string CreateHmacSha256Signature(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hash = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Validates request timestamp (prevents replay attacks)
        /// </summary>
        public static bool IsValidTimestamp(string timestamp, int toleranceMinutes = 5)
        {
            if (!long.TryParse(timestamp, out long timestampLong))
                return false;

            var requestTime = FromUnixTimeSeconds(timestampLong);
            var currentTime = DateTimeOffset.UtcNow;
            var timeDifference = Math.Abs((currentTime - requestTime).TotalMinutes);
            
            return timeDifference <= toleranceMinutes;
        }

        /// <summary>
        /// Helper method for .NET 4.5 compatibility
        /// </summary>
        private static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            return new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
        }

        /// <summary>
        /// Helper method for .NET 4.5 compatibility
        /// </summary>
        public static long ToUnixTimeSeconds(DateTimeOffset dateTime)
        {
            return (long)(dateTime - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
        }

        /// <summary>
        /// Validates API key format and basic structure
        /// </summary>
        public static bool IsValidApiKeyFormat(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            // API key should be at least 32 characters and contain only alphanumeric characters
            return apiKey.Length >= 32 && System.Text.RegularExpressions.Regex.IsMatch(apiKey, @"^[a-zA-Z0-9]+$");
        }

        /// <summary>
        /// Generates a secure random API key
        /// </summary>
        public static string GenerateApiKey(int length = 64)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new StringBuilder();
            
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            
            return result.ToString();
        }

        /// <summary>
        /// Generates a secure random secret key
        /// </summary>
        public static string GenerateSecretKey(int length = 128)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Gets client IP address considering proxy headers
        /// </summary>
        public static string GetClientIpAddress(HttpRequestBase request)
        {
            string clientIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            
            if (!string.IsNullOrEmpty(clientIp))
            {
                // Get the first IP if multiple IPs are present
                var firstIp = clientIp.Split(',')[0].Trim();
                if (IsValidIpAddress(firstIp))
                    return firstIp;
            }
            
            clientIp = request.ServerVariables["HTTP_X_REAL_IP"];
            if (!string.IsNullOrEmpty(clientIp) && IsValidIpAddress(clientIp))
                return clientIp;
            
            clientIp = request.ServerVariables["REMOTE_ADDR"];
            if (!string.IsNullOrEmpty(clientIp) && IsValidIpAddress(clientIp))
                return clientIp;
            
            return request.UserHostAddress;
        }

        /// <summary>
        /// Validates IP address format
        /// </summary>
        private static bool IsValidIpAddress(string ipAddress)
        {
            return System.Net.IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// Masks sensitive data for logging
        /// </summary>
        public static string MaskSensitiveData(string data, int visibleChars = 4)
        {
            if (string.IsNullOrEmpty(data))
                return data;

            if (data.Length <= visibleChars)
                return new string('*', data.Length);

            return data.Substring(0, visibleChars) + new string('*', data.Length - visibleChars);
        }
    }
}