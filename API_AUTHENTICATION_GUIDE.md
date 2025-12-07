# API Authentication Guide - Testing on Localhost

## ?? Authentication Overview

This API uses **SHA-256 based request signing** for authentication. Each request must include:

1. **API Key** - Identifies the client
2. **Timestamp** - Prevents replay attacks
3. **Signature** - Proves request authenticity

---

## ?? Test API Keys (Localhost Only)

For **localhost testing**, use these pre-configured API keys:

### Test API Key #1
```
API Key: TEST_API_KEY_12345
Secret Key: TEST_SECRET_KEY_67890
```

### Test API Key #2
```
API Key: DEV_API_KEY_ABCDE
Secret Key: DEV_SECRET_KEY_FGHIJ
```

### Test API Key #3 (Admin)
```
API Key: ADMIN_API_KEY_XYZ123
Secret Key: ADMIN_SECRET_KEY_ABC456
```

---

## ?? How SHA-256 Signature Works

### Algorithm
```
Signature = SHA256(ApiKey + Timestamp + RequestBody + SecretKey)
```

### Steps
1. Concatenate: `ApiKey + Timestamp + RequestBody + SecretKey`
2. Compute SHA-256 hash
3. Convert to hexadecimal string
4. Add to `X-Signature` header

---

## ?? Testing with Postman

### Step 1: Set Environment Variables

In Postman, create environment variables:

```
baseUrl = https://localhost:44300
apiKey = TEST_API_KEY_12345
secretKey = TEST_SECRET_KEY_67890
```

### Step 2: Add Pre-Request Script

Add this script to your collection or request:

```javascript
// Get timestamp
const timestamp = new Date().toISOString();

// Get request body (empty string if GET request)
const requestBody = pm.request.body && pm.request.body.raw ? pm.request.body.raw : '';

// Get variables
const apiKey = pm.environment.get('apiKey');
const secretKey = pm.environment.get('secretKey');

// Create signature string
const signatureString = apiKey + timestamp + requestBody + secretKey;

// Compute SHA-256
const signature = CryptoJS.SHA256(signatureString).toString();

// Set variables
pm.environment.set('timestamp', timestamp);
pm.environment.set('signature', signature);

console.log('API Key:', apiKey);
console.log('Timestamp:', timestamp);
console.log('Request Body:', requestBody);
console.log('Signature String:', signatureString);
console.log('Signature:', signature);
```

### Step 3: Add Headers

Add these headers to your requests:

```
X-API-Key: {{apiKey}}
X-Timestamp: {{timestamp}}
X-Signature: {{signature}}
Content-Type: application/json
```

---

## ?? Testing with cURL

### Example 1: Find Duplicate Voters (POST)

```bash
# Set variables
API_KEY="TEST_API_KEY_12345"
SECRET_KEY="TEST_SECRET_KEY_67890"
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
REQUEST_BODY='{"firstName":"Abhishek","lastName":"Bedkihale"}'

# Create signature
SIGNATURE_STRING="${API_KEY}${TIMESTAMP}${REQUEST_BODY}${SECRET_KEY}"
SIGNATURE=$(echo -n "$SIGNATURE_STRING" | openssl dgst -sha256 -hex | cut -d' ' -f2)

# Make request
curl -X POST https://localhost:44300/api/voters/find-duplicates \
  -H "Content-Type: application/json" \
  -H "X-API-Key: $API_KEY" \
  -H "X-Timestamp: $TIMESTAMP" \
  -H "X-Signature: $SIGNATURE" \
  -d "$REQUEST_BODY"
```

### Example 2: Get Verification Status (GET)

```bash
# Set variables
API_KEY="TEST_API_KEY_12345"
SECRET_KEY="TEST_SECRET_KEY_67890"
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
REQUEST_BODY=""

# Create signature
SIGNATURE_STRING="${API_KEY}${TIMESTAMP}${REQUEST_BODY}${SECRET_KEY}"
SIGNATURE=$(echo -n "$SIGNATURE_STRING" | openssl dgst -sha256 -hex | cut -d' ' -f2)

# Make request
curl -X GET https://localhost:44300/api/voters/verification-status \
  -H "X-API-Key: $API_KEY" \
  -H "X-Timestamp: $TIMESTAMP" \
  -H "X-Signature: $SIGNATURE"
```

---

## ?? Testing with PowerShell

### PowerShell Script

```powershell
# Configuration
$apiKey = "TEST_API_KEY_12345"
$secretKey = "TEST_SECRET_KEY_67890"
$baseUrl = "https://localhost:44300"
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

# Request body (use empty string for GET requests)
$requestBody = @{
    firstName = "Abhishek"
    lastName = "Bedkihale"
} | ConvertTo-Json -Compress

# Create signature
$signatureString = $apiKey + $timestamp + $requestBody + $secretKey
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$bytes = [System.Text.Encoding]::UTF8.GetBytes($signatureString)
$hash = $sha256.ComputeHash($bytes)
$signature = [System.BitConverter]::ToString($hash).Replace("-", "").ToLower()

# Create headers
$headers = @{
    "Content-Type" = "application/json"
    "X-API-Key" = $apiKey
    "X-Timestamp" = $timestamp
    "X-Signature" = $signature
}

# Make request
$response = Invoke-RestMethod -Uri "$baseUrl/api/voters/find-duplicates" `
    -Method Post `
    -Headers $headers `
    -Body $requestBody

# Display response
$response | ConvertTo-Json -Depth 10
```

### Save as `Test-VoterAPI.ps1`

Usage:
```powershell
.\Test-VoterAPI.ps1
```

---

## ?? C# Test Client

### C# Console Application

```csharp
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var apiKey = "TEST_API_KEY_12345";
        var secretKey = "TEST_SECRET_KEY_67890";
        var baseUrl = "https://localhost:44300";
        
        var client = new HttpClient();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var requestBody = "{\"firstName\":\"Abhishek\",\"lastName\":\"Bedkihale\"}";
        
        // Create signature
        var signatureString = apiKey + timestamp + requestBody + secretKey;
        var signature = ComputeSha256(signatureString);
        
        // Add headers
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        client.DefaultRequestHeaders.Add("X-Timestamp", timestamp);
        client.DefaultRequestHeaders.Add("X-Signature", signature);
        
        // Make request
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{baseUrl}/api/voters/find-duplicates", content);
        
        var result = await response.Content.ReadAsStringAsync();
        Console.WriteLine(result);
    }
    
    static string ComputeSha256(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
```

---

## ?? Testing with JavaScript/Node.js

### Node.js Script

```javascript
const crypto = require('crypto');
const https = require('https');

// Configuration
const apiKey = 'TEST_API_KEY_12345';
const secretKey = 'TEST_SECRET_KEY_67890';
const baseUrl = 'https://localhost:44300';
const timestamp = new Date().toISOString();

// Request body
const requestBody = JSON.stringify({
    firstName: 'Abhishek',
    lastName: 'Bedkihale'
});

// Create signature
const signatureString = apiKey + timestamp + requestBody + secretKey;
const signature = crypto.createHash('sha256').update(signatureString).digest('hex');

// Request options
const options = {
    hostname: 'localhost',
    port: 44300,
    path: '/api/voters/find-duplicates',
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-API-Key': apiKey,
        'X-Timestamp': timestamp,
        'X-Signature': signature
    },
    rejectUnauthorized: false // For self-signed certificates
};

// Make request
const req = https.request(options, (res) => {
    let data = '';
    res.on('data', (chunk) => data += chunk);
    res.on('end', () => console.log(JSON.parse(data)));
});

req.on('error', (e) => console.error(e));
req.write(requestBody);
req.end();
```

Save as `test-api.js` and run:
```bash
node test-api.js
```

---

## ?? Example Signatures

### GET Request (No Body)

```
API Key: TEST_API_KEY_12345
Timestamp: 2025-01-14T10:30:00Z
Request Body: (empty)
Secret Key: TEST_SECRET_KEY_67890

Signature String: TEST_API_KEY_123452025-01-14T10:30:00ZTEST_SECRET_KEY_67890
SHA-256 Hash: a1b2c3d4e5f6...
```

### POST Request (With Body)

```
API Key: TEST_API_KEY_12345
Timestamp: 2025-01-14T10:30:00Z
Request Body: {"firstName":"Abhishek"}
Secret Key: TEST_SECRET_KEY_67890

Signature String: TEST_API_KEY_123452025-01-14T10:30:00Z{"firstName":"Abhishek"}TEST_SECRET_KEY_67890
SHA-256 Hash: x1y2z3a4b5c6...
```

---

## ?? Common Issues

### Issue 1: 401 Unauthorized
**Cause**: Invalid signature
**Solution**: 
- Verify API key and secret key
- Check timestamp format (ISO 8601)
- Ensure request body matches exactly (no extra spaces)
- Verify signature string concatenation order

### Issue 2: Signature Mismatch
**Cause**: Different request body on client vs server
**Solution**:
- Use exact JSON string (no formatting)
- Don't add extra spaces or newlines
- Use same encoding (UTF-8)

### Issue 3: Timestamp Expired
**Cause**: Request too old
**Solution**:
- Generate fresh timestamp for each request
- Ensure system clock is synchronized

---

## ?? Debugging

### Enable Debug Logging

Add this to your request to see signature validation:

```javascript
console.log('=== Signature Debug ===');
console.log('API Key:', apiKey);
console.log('Timestamp:', timestamp);
console.log('Request Body:', requestBody);
console.log('Secret Key:', secretKey);
console.log('Signature String:', signatureString);
console.log('Computed Signature:', signature);
console.log('======================');
```

### Verify Signature Server-Side

Check server logs to see what signature the server computed and compare with your client signature.

---

## ?? Quick Reference

### Required Headers

| Header | Description | Example |
|--------|-------------|---------|
| X-API-Key | Client identifier | TEST_API_KEY_12345 |
| X-Timestamp | Request timestamp (UTC) | 2025-01-14T10:30:00Z |
| X-Signature | SHA-256 signature | a1b2c3d4e5f6... |
| Content-Type | Request format | application/json |

### Signature Formula

```
SHA256(ApiKey + Timestamp + RequestBody + SecretKey)
```

**For GET requests**: RequestBody = "" (empty string)  
**For POST requests**: RequestBody = JSON string (no formatting)

---

## ?? Test Scenarios

### Scenario 1: Find Duplicates
```
Endpoint: POST /api/voters/find-duplicates
Body: {"firstName":"Abhishek","lastName":"Bedkihale"}
Expected: 200 OK with duplicate records
```

### Scenario 2: Get Status
```
Endpoint: GET /api/voters/verification-status
Body: (none)
Expected: 200 OK with statistics
```

### Scenario 3: Mark Duplicates
```
Endpoint: POST /api/voters/mark-duplicates
Body: {"srNoArray":[201,202],"isDuplicate":true,"remarks":"Test"}
Expected: 200 OK with duplication ID
```

---

## ?? Quick Start Commands

### Test with PowerShell (Windows)
```powershell
# Download test script
Invoke-WebRequest -Uri "URL_TO_SCRIPT" -OutFile "Test-API.ps1"

# Run test
.\Test-API.ps1
```

### Test with cURL (Linux/Mac)
```bash
# Set environment
export API_KEY="TEST_API_KEY_12345"
export SECRET_KEY="TEST_SECRET_KEY_67890"

# Run test
./test-api.sh
```

### Test with Postman
```
1. Import collection
2. Set environment variables
3. Run request
```

---

## ?? Support

If signature validation fails:

1. **Check API Key** - Must match server configuration
2. **Check Timestamp Format** - Must be ISO 8601 UTC
3. **Check Request Body** - Must be exact JSON string
4. **Check Secret Key** - Must match server configuration
5. **Check Concatenation** - ApiKey + Timestamp + Body + Secret

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Status**: ? Ready for Testing
