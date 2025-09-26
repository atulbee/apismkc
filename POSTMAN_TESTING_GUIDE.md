# SMKC Banking API - Postman Testing Guide

## Overview
This guide provides comprehensive instructions for testing the SMKC Banking API using Postman. The API uses SHA-256 HMAC authentication for secure communication.

## Authentication Requirements

### Required Headers
All API requests must include the following headers:

1. **X-API-Key**: Your assigned API key
2. **X-Timestamp**: Current Unix timestamp (seconds since epoch)
3. **X-Signature**: HMAC-SHA256 signature of the request
4. **Content-Type**: application/json (for POST requests)

### Signature Calculation
The signature is calculated using HMAC-SHA256 with the following string:
```
HTTP_METHOD + URI + QUERY_STRING + TIMESTAMP + REQUEST_BODY
```

**Example Signature String for GET /api/accounts/ACC1234567890:**
```
GET/api/accounts/ACC123456789017270929305
```

**Example Signature String for POST /api/accounts:**
```
POST/api/accounts1727092930{"customerReference":"CUST001","accountType":"Savings","currency":"USD","branchCode":"001"}
```

### Sample JavaScript for Signature Generation (Postman Pre-request Script)
```javascript
// Set variables
const apiKey = pm.environment.get("api_key_valid");
const secretKey = pm.environment.get("secret_key");
const timestamp = Math.floor(Date.now() / 1000).toString();
const method = pm.request.method;
const url = pm.request.url.getPath();
const queryString = pm.request.url.getQueryString() || '';
const body = pm.request.body && pm.request.body.raw ? pm.request.body.raw : '';

// Create signature string
const signatureString = method + url + queryString + timestamp + body;

// Calculate HMAC-SHA256 signature
const signature = CryptoJS.HmacSHA256(signatureString, secretKey).toString();

// Set headers
pm.request.headers.add({key: 'X-API-Key', value: apiKey});
pm.request.headers.add({key: 'X-Timestamp', value: timestamp});
pm.request.headers.add({key: 'X-Signature', value: signature});

console.log('Signature String:', signatureString);
console.log('Generated Signature:', signature);
```

## Environment Variables Setup

### Required Variables
Create a Postman environment with the following variables:

```json
{
  "base_url": "https://localhost:44300/api",
  "api_key_valid": "BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5",
  "api_key_invalid": "INVALID_KEY_12345",
  "secret_key": "S3cur3K3y!B4nk001#2024$Pr0d&V3ryL0ngS3cr3tK3yF0rB4nk1ng"
}
```

### Test Data
The API includes mock data for testing:

**Sample Accounts:**
- ACC1234567890 (Active, Balance: $15,000.50)
- ACC2345678901 (Active, Balance: $25,750.75)
- ACC3456789012 (Active, Balance: $5,250.25)

**Sample Customers:**
- CUST001 (John Doe)
- CUST002 (Jane Smith)
- CUST003 (Bob Johnson)

**Sample Transactions:**
- TXN001, TXN002, TXN003 (Various types and amounts)

## API Endpoints Reference

### Account Management
- **GET** `/accounts/{accountNumber}` - Retrieve account details
- **GET** `/accounts/{accountNumber}/balance` - Get account balance
- **POST** `/accounts` - Create new account

### Customer Management
- **GET** `/customers/{customerReference}` - Retrieve customer details
- **POST** `/customers` - Create new customer

### Transaction Processing
- **GET** `/transactions/{transactionId}` - Get transaction details
- **POST** `/transactions` - Process new transaction
- **POST** `/transactions/history` - Get transaction history

## Response Status Codes

### Success Responses
- **200 OK**: Request successful
- **201 Created**: Resource created successfully

### Client Error Responses
- **400 Bad Request**: Invalid request data or validation error
- **401 Unauthorized**: Authentication failed (invalid API key, signature, or timestamp)
- **403 Forbidden**: Access denied (IP not whitelisted)
- **404 Not Found**: Resource not found
- **429 Too Many Requests**: Rate limit exceeded

### Server Error Responses
- **500 Internal Server Error**: Server-side error occurred

## Rate Limiting
The API implements rate limiting with the following limits:
- **100 requests per minute** per API key
- Rate limit headers are included in responses:
  - `X-RateLimit-Limit`: Maximum requests allowed
  - `X-RateLimit-Remaining`: Remaining requests in current window
  - `X-RateLimit-Reset`: Unix timestamp when limit resets
  - `Retry-After`: Seconds to wait before retrying (included in 429 responses)

## Security Features
1. **SHA-256 HMAC Authentication**: All requests must be signed
2. **Timestamp Validation**: Requests older than 5 minutes are rejected
3. **IP Whitelisting**: Only approved IP addresses can access the API
4. **Rate Limiting**: Prevents API abuse
5. **HTTPS Only**: All communication must use HTTPS in production

## Testing Scenarios

### 1. Successful Operations
- Test all endpoints with valid authentication
- Verify response structure and data accuracy
- Check success status codes (200, 201)

### 2. Authentication Failures
- Test with missing API key (401)
- Test with invalid API key (401)
- Test with invalid signature (401)
- Test with expired timestamp (401)

### 3. Validation Errors
- Test with missing required fields (400)
- Test with invalid data formats (400)
- Test with invalid account numbers/references (404)

### 4. Rate Limiting
- Send multiple rapid requests to trigger rate limiting (429)
- Verify rate limit headers in responses
- Test retry behavior after rate limit reset

### 5. IP Restrictions
- Test from non-whitelisted IP addresses (403)

## Error Response Format
All error responses follow this structure:
```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "timestamp": "2025-09-24T15:45:30Z",
  "requestId": "req_123456789"
}
```

## Troubleshooting

### Common Issues
1. **401 Unauthorized**: Check API key, signature calculation, and timestamp
2. **403 Forbidden**: Verify IP address is whitelisted
3. **429 Too Many Requests**: Wait for rate limit reset or implement retry logic
4. **500 Internal Server Error**: Check server logs and contact support

### Signature Debugging
- Log the signature string before hashing
- Verify timestamp format (Unix seconds, not milliseconds)
- Ensure request body is included for POST requests
- Check that the secret key matches the server configuration

### Testing Tips
1. Use Postman's pre-request scripts for automatic signature generation
2. Set up environment variables for easy switching between environments
3. Create test suites for automated regression testing
4. Monitor response times and implement appropriate timeouts
5. Test edge cases and boundary conditions

## Production Considerations
- Use HTTPS endpoints only
- Implement proper error handling and retry logic
- Monitor rate limits and implement backoff strategies
- Secure storage of API keys and secret keys
- Log all API interactions for audit purposes
- Implement circuit breaker patterns for fault tolerance