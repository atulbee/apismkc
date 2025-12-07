# Park Booking API - Quick Reference

## Project Structure Summary

```
SmkcApi/
??? Controllers/
?   ??? CitizenController.cs         - POST /api/citizen/register, verify-otp, resend-otp
?   ??? SlotsController.cs           - GET /api/slots/available
?   ??? BookingsController.cs        - POST/GET /api/bookings/*, /receipt
?   ??? DepartmentController.cs      - POST /api/department/login, GET /api/department/*
?   ??? UtilitiesController.cs       - POST /api/utils/send-sms, /generate-qr
?
??? Services/
?   ??? IParkBookingService.cs       - Business logic interface
?   ??? ParkBookingService.cs        - Implementation (40+ lines of validation)
?
??? Repositories/
?   ??? IParkBookingRepository.cs    - Data access interface
?   ??? ParkBookingRepository.cs     - Oracle stored procedure calls
?
??? Models/
?   ??? ParkBookingModels.cs         - 30+ request/response classes
?
??? App_Start/
    ??? SimpleDependencyResolver.cs  - Dependency injection container
```

## Quick API Reference

### Citizen APIs
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/citizen/register` | Register new citizen, send OTP |
| POST | `/api/citizen/verify-otp` | Verify OTP, get auth token |
| POST | `/api/citizen/resend-otp` | Resend OTP to mobile |

### Slots APIs
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/slots/available?date=YYYY-MM-DD` | Get available time slots |

### Bookings APIs
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/bookings/create` | Create new booking |
| GET | `/api/bookings/{bookingId}` | Get booking details |
| GET | `/api/bookings/{bookingId}/receipt` | Download booking receipt |

### Department APIs
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/department/login` | Department user login |
| GET | `/api/department/dashboard/stats` | Get dashboard statistics |
| GET | `/api/department/bookings` | Get all bookings (paginated) |
| GET | `/api/department/bookings/search/{token}` | Search booking by token |
| POST | `/api/department/bookings/{bookingId}/verify` | Verify citizen entry |
| POST | `/api/department/bookings/{bookingId}/cancel` | Cancel booking |

### Utility APIs
| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/utils/send-sms` | Send SMS notification |
| POST | `/api/utils/generate-qr` | Generate QR code for booking |
| GET | `/api/reports/bookings` | Generate booking report |

## Response Format (All Endpoints)

```json
{
  "success": true/false,
  "message": "Description",
  "data": { /* varies by endpoint */ },
  "errorCode": "ERROR_CODE", /* only on error */
  "timestamp": "ISO-8601 UTC",
  "requestId": "GUID"
}
```

## Validation Rules

### Mobile Number
- Exactly 10 digits
- Pattern: `^\d{10}$`

### OTP
- 4 digits
- Valid for 5 minutes
- 3 resend attempts allowed

### Dates
- Format: YYYY-MM-DD
- No past dates (for bookings)
- Start < End (for reports)

## Security Features

- ? SHA-256 request signing
- ? IP whitelist validation
- ? Rate limiting (50-100 req/min)
- ? Security headers
- ? Async/await for performance
- ? Sensitive data masking in logs

## Key Classes

### CitizenController
- `Register()` - Citizen registration
- `VerifyOtp()` - OTP verification
- `ResendOtp()` - Resend OTP

### SlotsController
- `GetAvailableSlots()` - Get slots for date

### BookingsController
- `CreateBooking()` - Create booking
- `GetBookingDetails()` - Get details
- `DownloadReceipt()` - Generate receipt

### DepartmentController
- `Login()` - Department login
- `GetDashboardStats()` - Dashboard stats
- `GetAllBookings()` - List bookings
- `SearchBookingByToken()` - Search by token
- `VerifyEntry()` - Verify citizen entry
- `CancelBooking()` - Cancel booking

### ParkBookingService
- Validates all inputs
- Calls repository for data
- Returns ApiResponse<T>

### ParkBookingRepository
- Executes Oracle stored procedures
- Maps resultsets to models
- Handles connection management

## Error Codes

| Code | HTTP Status | Meaning |
|------|-------------|---------|
| MISSING_REQUEST | 400 | Request body required |
| INVALID_MOBILE | 400 | Invalid mobile format |
| INVALID_OTP | 401 | Invalid/expired OTP |
| SLOT_FULL | 409 | No seats available |
| BOOKING_NOT_FOUND | 404 | Booking doesn't exist |
| INVALID_CREDENTIALS | 401 | Wrong username/password |

## Configuration

### Web.config Settings Required

```xml
<connectionStrings>
  <add name="OracleDb" 
       connectionString="Data Source=...;User Id=...;Password=...;" />
</connectionStrings>

<appSettings>
  <add key="Sms_User" value="..." />
  <add key="Sms_Password" value="..." />
  <add key="Sms_SenderId" value="ParkBooking" />
  <add key="Sms_Channel" value="Trans" />
</appSettings>
```

## Common Request Headers

```
Content-Type: application/json
Authorization: Bearer YOUR_API_KEY
X-Request-ID: (auto-generated)
X-API-Key: (for SHA authentication)
```

## Common Response Headers

```
X-Request-ID: GUID (unique request identifier)
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Cache-Control: no-store, no-cache, must-revalidate
```

## Testing Checklist

- [ ] Mobile validation (10 digits)
- [ ] OTP validation (4 digits, 5 min expiry)
- [ ] Slot availability check
- [ ] Booking creation with all validations
- [ ] Department login with credentials
- [ ] Pagination on list endpoints
- [ ] Error handling with proper HTTP status
- [ ] Security headers in responses
- [ ] Rate limiting functionality
- [ ] Request ID generation and tracking

## Performance Tips

1. Use async/await for all long-running operations
2. Connection pooling is automatic via OracleConnectionFactory
3. Implement caching for available slots
4. Use pagination (max 100 items per page)
5. Keep stored procedures optimized with proper indexes

## Logging

All errors logged with:
- Timestamp (UTC)
- Action name
- Error message
- Masked API keys
- Request ID for tracing

Example:
```
2025-11-14 10:30:00 UTC - CITIZEN_CONTROLLER_REGISTER_ERROR: Invalid mobile format
2025-11-14 10:30:00 UTC - SECURITY_ERROR - Action: Register, ApiKey: ****, RequestId: guid
```

## Dependencies

- Oracle.ManagedDataAccess.Client (NuGet)
- System.Web.Http (built-in)
- System.Data (built-in)

## Oracle Procedures Used

1. `PKG_PARK_BOOKING.SP_CITIZEN_REGISTER` - Register citizen
2. `PKG_PARK_BOOKING.SP_VERIFY_OTP` - Verify OTP
3. `PKG_PARK_BOOKING.SP_RESEND_OTP` - Resend OTP
4. `PKG_PARK_BOOKING.SP_GET_AVAILABLE_SLOTS` - Get slots
5. `PKG_PARK_BOOKING.SP_CREATE_BOOKING` - Create booking
6. `PKG_PARK_BOOKING.SP_GET_BOOKING_DETAILS` - Get booking
7. `PKG_PARK_BOOKING.SP_GET_ALL_BOOKINGS` - List bookings
8. `PKG_PARK_BOOKING.SP_SEARCH_BOOKING_BY_TOKEN` - Search booking
9. `PKG_PARK_BOOKING.SP_VERIFY_ENTRY` - Verify entry
10. `PKG_PARK_BOOKING.SP_CANCEL_BOOKING` - Cancel booking
11. `PKG_PARK_BOOKING.SP_DEPARTMENT_LOGIN` - Department login
12. `PKG_PARK_BOOKING.SP_GET_DASHBOARD_STATS` - Dashboard stats
13. `PKG_PARK_BOOKING.SP_GENERATE_RECEIPT` - Generate receipt
14. `PKG_PARK_BOOKING.SP_GENERATE_BOOKING_REPORT` - Generate report
15. `PKG_PARK_BOOKING.SP_LOG_SMS` - Log SMS

## Next Steps

1. Deploy to Windows Server 2012 R2
2. Configure Oracle database connection
3. Deploy Oracle stored procedures
4. Configure SSL certificate for HTTPS
5. Set up rate limiting thresholds
6. Configure IP whitelist
7. Implement monitoring and alerting
8. Create API documentation (Swagger)
9. Set up logging framework
10. Implement caching strategy
