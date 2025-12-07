# ?? CORRECTED TEST CREDENTIALS - IMPORTANT UPDATE

## ?? Issue Resolved

The initial test credentials were **too short** and caused "Invalid API Key format" error.

The `ApiKeyAuthenticationHandler` requires:
- ? API keys must be **at least 32 characters**
- ? Only alphanumeric and underscore allowed: `[A-Za-z0-9_]`
- ? Signature uses **HMAC-SHA256** (not plain SHA-256)
- ? Timestamp must be **Unix timestamp** (not ISO 8601)

---

## ? CORRECTED TEST CREDENTIALS

### For Localhost Testing (http://localhost:57031)

```
API Key:    TEST_API_KEY_12345678901234567890123456789012
Secret Key: TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890
Base URL:   http://localhost:57031
```

### Alternative Test Keys

```
API Key:    DEV_API_KEY_ABCDE67890FGHIJ12345KLMNO67890
Secret Key: DEV_SECRET_KEY_FGHIJ67890KLMNO12345PQRST

API Key:    ADMIN_API_KEY_XYZ12345678901234567890ABC456
Secret Key: ADMIN_SECRET_KEY_ABC45678901234567890DEF
```

---

## ?? Signature Algorithm (CORRECTED)

### Important Changes:
1. **Timestamp**: Use Unix timestamp (not ISO 8601)
2. **Signature**: Use HMAC-SHA256 with Base64 encoding (not plain SHA-256 hex)
3. **String to Sign**: `HTTP_METHOD + REQUEST_URI + REQUEST_BODY + TIMESTAMP + API_KEY`

### Correct Algorithm:
```javascript
// 1. Get Unix timestamp (seconds since epoch)
const timestamp = Math.floor(Date.now() / 1000).toString();

// 2. Build string to sign
const httpMethod = 'POST';  // or 'GET'
const requestUri = '/api/voters/find-duplicates';
const requestBody = '{"firstName":"Abhishek"}';  // or '' for GET
const apiKey = 'TEST_API_KEY_12345678901234567890123456789012';
const secretKey = 'TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890';

const stringToSign = httpMethod + requestUri + requestBody + timestamp + apiKey;

// 3. Compute HMAC-SHA256 signature
const signature = CryptoJS.HmacSHA256(stringToSign, secretKey).toString(CryptoJS.enc.Base64);

// 4. Add headers to request
headers: {
  'X-API-Key': apiKey,
  'X-Timestamp': timestamp,
  'X-Signature': signature,
  'Content-Type': 'application/json'
}
```

### Example:
```
HTTP Method: POST
Request URI: /api/voters/find-duplicates
Request Body: {"firstName":"Abhishek"}
Timestamp: 1705232400
API Key: TEST_API_KEY_12345678901234567890123456789012

String to Sign: POST/api/voters/find-duplicates{"firstName":"Abhishek"}1705232400TEST_API_KEY_12345678901234567890123456789012

HMAC-SHA256 with Secret: TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890
Result (Base64): xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

---

## ?? ALL FILES UPDATED

The following files have been updated with correct credentials:

### ? Updated Files:
1. `Security/ApiKeyAuthenticationHandler.cs` - Added test API keys
2. `Postman/DuplicateVoterAPI.postman_collection.json` - Updated credentials & signature
3. `Postman/DuplicateVoterAPI.postman_environment.json` - Updated credentials
4. `TestClient.html` - Updated credentials & signature
5. `Test-VoterAPI.ps1` - Updated credentials & signature

### ?? What Changed:
- ? API keys now 32+ characters
- ? Signature uses HMAC-SHA256 + Base64
- ? Timestamp uses Unix format
- ? Base URL uses HTTP (localhost:57031)

---

## ?? Test Now - Postman (READY!)

### Step 1: Re-import Collection
```
1. Delete old collection (if imported)
2. Import: Postman/DuplicateVoterAPI.postman_collection.json
3. Credentials are PRE-CONFIGURED
```

### Step 2: Test Any Endpoint
```
1. Select "1. Find Duplicate Voters"
2. Click "Send"
3. Should work! ?
```

### What the Pre-Request Script Does:
```javascript
? Generates Unix timestamp automatically
? Computes HMAC-SHA256 signature automatically
? Uses correct API key format
? Builds string to sign correctly
? Adds all required headers
```

---

## ?? Test Now - HTML Client (READY!)

### Step 1: Open File
```
1. Open TestClient.html in browser
2. Credentials are PRE-FILLED
```

### Step 2: Test
```
1. Click any endpoint card
2. Click "Test" button
3. Should work! ?
```

---

## ?? Test Now - PowerShell (READY!)

### Run All Tests:
```powershell
.\Test-VoterAPI.ps1
```

### Run Specific Endpoint:
```powershell
.\Test-VoterAPI.ps1 -Endpoint "verification-status" -Method "GET"
```

---

## ?? Required Headers (For Manual Testing)

```
Content-Type: application/json
X-API-Key: TEST_API_KEY_12345678901234567890123456789012
X-Timestamp: 1705232400
X-Signature: [HMAC-SHA256 Base64 signature]
```

---

## ?? Debugging

### If You Still Get "Invalid API Key format":
1. ? Check API key is exactly: `TEST_API_KEY_12345678901234567890123456789012`
2. ? Check it's at least 32 characters
3. ? Check no special characters except underscore

### If You Get "Invalid signature":
1. ? Check you're using **HMAC-SHA256** (not SHA-256)
2. ? Check signature is **Base64 encoded** (not hex)
3. ? Check string to sign format: `METHOD + URI + BODY + TIMESTAMP + APIKEY`
4. ? Check timestamp is **Unix timestamp** (not ISO 8601)

### If You Get "Invalid or expired timestamp":
1. ? Use Unix timestamp: `Math.floor(Date.now() / 1000)`
2. ? Check system clock is correct
3. ? Timestamp valid for 5 minutes

---

## ? Production API Keys (Already Configured)

These production keys are already in the handler:

```
Bank 001:
  API Key: BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5
  Secret:  S3cur3K3y!B4nk001#2024$Pr0d&V3ryL0ngS3cr3tK3yF0rB4nk1ng

Bank 002:
  API Key: BANK002_9c6f3a8e1d4b7c0f2e5a8b1c4d7f0a3e6b9c2d5f8a1b4c7e0d3f6a9b2c5d8e1f4
  Secret:  An0th3rS3cur3K3y!B4nk002#2024$V3ryS3cr3tK3yF0rB4nk2ng
```

---

## ?? Quick Test Commands

### cURL (Linux/Mac):
```bash
# Set variables
API_KEY="TEST_API_KEY_12345678901234567890123456789012"
SECRET_KEY="TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890"
TIMESTAMP=$(date +%s)
METHOD="GET"
URI="/api/voters/verification-status"
BODY=""

# Create signature
STRING_TO_SIGN="${METHOD}${URI}${BODY}${TIMESTAMP}${API_KEY}"
SIGNATURE=$(echo -n "$STRING_TO_SIGN" | openssl dgst -sha256 -hmac "$SECRET_KEY" -binary | base64)

# Make request
curl -X GET http://localhost:57031/api/voters/verification-status \
  -H "X-API-Key: $API_KEY" \
  -H "X-Timestamp: $TIMESTAMP" \
  -H "X-Signature: $SIGNATURE"
```

### PowerShell (Windows):
```powershell
.\Test-VoterAPI.ps1 -Endpoint "verification-status" -Method "GET"
```

---

## ?? Support

### Files to Check:
1. `Security/ApiKeyAuthenticationHandler.cs` - Authentication logic
2. `Security/SecurityHelper.cs` - Signature algorithm
3. This file - Correct credentials

### Debug Tips:
- Enable console logging in Postman (View ? Show Postman Console)
- Check browser console in HTML client (F12)
- Review PowerShell output for debug info

---

## ? STATUS

```
? API Keys: Updated (32+ chars)
? Signature: Fixed (HMAC-SHA256 + Base64)
? Timestamp: Fixed (Unix timestamp)
? Base URL: Updated (http://localhost:57031)
? All Files: Updated
? Ready to Test: YES!
```

---

**Import Postman collection and test now - it should work! ??**
