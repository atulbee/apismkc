# SMKC Banking API - .NET 4.5

A secure, robust Web API designed for banking integration with SHA-based authentication and comprehensive security features.

## 🔒 Security Features

- **SHA-256 Authentication**: Request signing with HMAC-SHA256
- **API Key Management**: Secure API key validation
- **IP Whitelisting**: Restrict access to authorized IP addresses
- **Rate Limiting**: Protect against abuse and DoS attacks
- **Request Validation**: Timestamp validation to prevent replay attacks
- **Secure Headers**: OWASP-compliant security headers
- **HTTPS Enforcement**: TLS/SSL encryption required

## 🏗️ Architecture

### Controllers
- **AccountController**: Account management operations
- **CustomerController**: Customer information management  
- **TransactionController**: Transaction processing and history

### Security Layers
- **ApiKeyAuthenticationHandler**: Primary authentication mechanism
- **ShaAuthenticationAttribute**: SHA-based request validation
- **IPWhitelistAttribute**: IP address filtering
- **RateLimitAttribute**: Request rate limiting

### Business Logic
- **Services**: Business logic layer with interfaces
- **Repositories**: Data access layer (in-memory for demo)
- **Models**: Data transfer objects and domain models

## 🚀 Getting Started

### Prerequisites
- .NET Framework 4.5 or higher
- Windows Server 2012 R2 or higher
- IIS 8.0 or higher (for production deployment)

### Installation

1. **Clone or extract the project**
2. **Configure IIS** (for production):
   ```
   - Create new website in IIS Manager
   - Point to the project directory
   - Set Application Pool to .NET Framework v4.0
   - Enable HTTPS with valid SSL certificate
   ```

3. **Update Web.config**:
   - Configure connection strings
   - Update allowed CORS origins
   - Set environment-specific settings

### Configuration

#### API Keys
Update the API keys and secrets in `ApiKeyAuthenticationHandler.cs`:
```csharp
// Replace with your bank's actual API keys
private static readonly HashSet<string> ValidApiKeys = new HashSet<string>
{
    "YOUR_BANK_API_KEY_HERE"
};

private static readonly Dictionary<string, string> ApiKeySecrets = new Dictionary<string, string>
{
    ["YOUR_API_KEY"] = "YOUR_SECRET_KEY"
};
```

#### IP Whitelist
Configure allowed IP addresses in `IPWhitelistAttribute.cs`:
```csharp
private static readonly HashSet<string> WhitelistedIPs = new HashSet<string>
{
    "192.168.1.100",        // Bank's IP
    "203.0.113.0/24"        // Bank's IP range
};
```

## 📚 API Documentation

### Base URL
- Development: `https://localhost:44300/api`
- Production: `https://your-domain.com/api`

### Authentication Headers
All API requests must include these headers:
```
X-API-Key: YOUR_API_KEY
X-Timestamp: UNIX_TIMESTAMP
X-Signature: SHA256_HMAC_SIGNATURE
```

### Signature Generation
```csharp
string stringToSign = httpMethod + requestUri + requestBody + timestamp + apiKey;
string signature = HMAC_SHA256(stringToSign, secretKey);
```

### Endpoints

#### Account Management
- `GET /api/accounts/{accountNumber}` - Get account details
- `GET /api/accounts/{accountNumber}/balance` - Get account balance
- `GET /api/accounts/customer/{customerReference}` - Get customer accounts
- `POST /api/accounts` - Create new account
- `PUT /api/accounts/{accountNumber}/status` - Update account status

#### Customer Management
- `GET /api/customers/{customerReference}` - Get customer details
- `POST /api/customers` - Create new customer
- `PUT /api/customers` - Update customer information
- `PUT /api/customers/{customerReference}/kyc` - Update KYC status

#### Transaction Processing
- `GET /api/transactions/{transactionId}` - Get transaction details
- `POST /api/transactions` - Process new transaction
- `POST /api/transactions/history` - Get transaction history
- `POST /api/transactions/{transactionId}/reverse` - Reverse transaction

### Response Format
All responses follow this structure:
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { ... },
  "timestamp": "2025-09-24T10:30:00Z",
  "requestId": "uuid-here"
}
```

### Error Handling
Error responses include:
```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "timestamp": "2025-09-24T10:30:00Z",
  "requestId": "uuid-here"
}
```

## 🔍 Sample Requests

### Create Account
```http
POST /api/accounts
X-API-Key: BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5
X-Timestamp: 1695542400
X-Signature: calculated_signature_here

{
  "customerReference": "CUST001",
  "accountType": "Savings",
  "currency": "USD",
  "branchCode": "001"
}
```

### Process Transaction
```http
POST /api/transactions
X-API-Key: BANK001_4f8b2c7d9e3a1f6b8c5d0e9a2f7b4c8d1e6f9a3b7c2d5e8f0a9b6c3d7e1f4a8b5
X-Timestamp: 1695542400
X-Signature: calculated_signature_here

{
  "fromAccount": "ACC1234567890",
  "toAccount": "ACC2345678901",
  "amount": 1000.00,
  "currency": "USD",
  "description": "Transfer payment",
  "referenceNumber": "REF123456"
}
```

## 🛡️ Security Best Practices

1. **Always use HTTPS** in production
2. **Rotate API keys** regularly
3. **Monitor security logs** for suspicious activity
4. **Implement proper firewall** rules
5. **Keep framework** and dependencies updated
6. **Use secure connection strings** with encryption
7. **Enable Windows Event Logging** for audit trails

## 📊 Monitoring & Logging

### Log Locations
- Application logs: `C:\Logs\SmkcApi\trace.log`
- Windows Event Log: `Application` source `SMKC API`

### Key Metrics to Monitor
- Authentication failures
- Rate limit violations
- IP whitelist violations
- Transaction processing errors
- Response times

## 🚧 Production Deployment

### IIS Configuration
1. Install .NET Framework 4.5+
2. Create application pool (Integrated mode, .NET 4.0)
3. Configure SSL certificate
4. Set appropriate file permissions
5. Configure request filtering
6. Enable compression

### Security Hardening
1. Remove unnecessary HTTP headers
2. Disable unnecessary HTTP methods
3. Configure request size limits
4. Enable HTTP Strict Transport Security
5. Implement Content Security Policy
6. Configure proper error pages

## 📞 Support

For technical support and integration assistance:
- Email: api-support@smkc.com
- Documentation: https://api.smkc.com/docs
- Status Page: https://status.smkc.com

## 📄 License

This software is proprietary and confidential. Unauthorized copying, modification, or distribution is strictly prohibited.

---
**Version**: 1.0  
**Last Updated**: September 24, 2025  
**Framework**: .NET Framework 4.5  
**Minimum Server**: Windows Server 2012 R2