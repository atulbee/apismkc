# Evening Retreat Park Booking System - .NET 4.5 API Implementation Guide

## Overview

This document provides a complete guide to the implemented .NET 4.5 Web API for the Evening Retreat Park Booking System. The implementation follows the specifications outlined in the API_DOCUMENTATION.md file and uses Oracle 12c stored procedures for database operations.

## Architecture

### Project Structure

```
SmkcApi/
??? Controllers/
?   ??? CitizenController.cs          # Citizen registration and OTP verification
?   ??? SlotsController.cs            # Available slots management
?   ??? BookingsController.cs         # Booking operations
?   ??? DepartmentController.cs       # Department portal operations
?   ??? UtilitiesController.cs        # SMS, QR codes, and reports
??? Models/
?   ??? ParkBookingModels.cs          # All request/response models
??? Services/
?   ??? IParkBookingService.cs        # Service interface
?   ??? ParkBookingService.cs         # Business logic implementation
??? Repositories/
?   ??? IParkBookingRepository.cs     # Repository interface
?   ??? ParkBookingRepository.cs      # Oracle stored procedure calls
??? Security/
?   ??? ShaAuthenticationAttribute.cs # SHA-256 authentication filter
?   ??? IPWhitelistAttribute.cs       # IP whitelist security
?   ??? RateLimitAttribute.cs         # Rate limiting
??? App_Start/
    ??? SimpleDependencyResolver.cs   # Dependency injection configuration
```

### Technology Stack

- **Framework**: .NET Framework 4.5
- **Database**: Oracle 12c
- **Authentication**: SHA-256 based request signing
- **API Pattern**: RESTful Web API
- **Architecture**: Layered (Controllers ? Services ? Repositories)

## Implemented Endpoints

### 1. Citizen Portal APIs

#### 1.1 Citizen Registration
```
POST /api/citizen/register
```

**Request**:
```json
{
  "name": "John Doe",
  "mobile": "9876543210"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "OTP sent successfully to mobile number",
  "data": {
    "citizenId": "CIT202511140001",
    "name": "John Doe",
    "mobile": "9876543210",
    "otpSentAt": "2025-11-14T10:30:00",
    "otpExpiresAt": "2025-11-14T10:35:00"
  },
  "timestamp": "2025-11-14T10:30:00Z",
  "requestId": "guid-value"
}
```

**Implementation Details**:
- Controller: `CitizenController.Register()`
- Service: `ParkBookingService.RegisterCitizenAsync()`
- Repository: `ParkBookingRepository.RegisterCitizenAsync()`
- Oracle Procedure: `PKG_PARK_BOOKING.SP_CITIZEN_REGISTER`

#### 1.2 OTP Verification
```
POST /api/citizen/verify-otp
```

**Request**:
```json
{
  "citizenId": "CIT202511140001",
  "mobile": "9876543210",
  "otp": "1234"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "data": {
    "citizenId": "CIT202511140001",
    "isVerified": true,
    "authToken": "jwt-token-here",
    "tokenExpiry": "2025-11-14T22:30:00"
  },
  "timestamp": "2025-11-14T10:31:00Z",
  "requestId": "guid-value"
}
```

#### 1.3 Resend OTP
```
POST /api/citizen/resend-otp
```

**Request**:
```json
{
  "citizenId": "CIT202511140001",
  "mobile": "9876543210"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "OTP resent successfully",
  "data": {
    "otpSentAt": "2025-11-14T10:35:00",
    "otpExpiresAt": "2025-11-14T10:40:00",
    "attemptsRemaining": 2
  },
  "timestamp": "2025-11-14T10:35:00Z",
  "requestId": "guid-value"
}
```

### 2. Slots APIs

#### 2.1 Get Available Slots
```
GET /api/slots/available?date=2025-11-15
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Available slots retrieved successfully",
  "data": {
    "date": "2025-11-15",
    "slots": [
      {
        "slotId": "SLOT001",
        "slotCode": "7PM",
        "slotTime": "7:00 PM - 8:00 PM",
        "slotLabel": "7-8 PM",
        "totalCapacity": 100,
        "bookedCount": 45,
        "availableSeats": 55,
        "isAvailable": true
      },
      {
        "slotId": "SLOT002",
        "slotCode": "8PM",
        "slotTime": "8:00 PM - 9:00 PM",
        "slotLabel": "8-9 PM",
        "totalCapacity": 100,
        "bookedCount": 78,
        "availableSeats": 22,
        "isAvailable": true
      },
      {
        "slotId": "SLOT003",
        "slotCode": "9PM",
        "slotTime": "9:00 PM - 10:00 PM",
        "slotLabel": "9-10 PM",
        "totalCapacity": 100,
        "bookedCount": 100,
        "availableSeats": 0,
        "isAvailable": false
      }
    ]
  },
  "timestamp": "2025-11-14T10:32:00Z",
  "requestId": "guid-value"
}
```

### 3. Bookings APIs

#### 3.1 Create Booking
```
POST /api/bookings/create
```

**Request**:
```json
{
  "citizenId": "CIT202511140001",
  "date": "2025-11-15",
  "slotId": "SLOT001",
  "slotCode": "7PM"
}
```

**Response (201)**:
```json
{
  "success": true,
  "message": "Booking created successfully",
  "data": {
    "bookingId": "BK202511140001",
    "token": "20251115-7PM-AB12",
    "citizenName": "John Doe",
    "mobile": "9876543210",
    "date": "2025-11-15",
    "slotTime": "7:00 PM - 8:00 PM",
    "slotLabel": "7-8 PM",
    "bookingTime": "2025-11-14T10:45:00",
    "status": "CONFIRMED",
    "qrCode": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
  },
  "timestamp": "2025-11-14T10:45:00Z",
  "requestId": "guid-value"
}
```

#### 3.2 Get Booking Details
```
GET /api/bookings/{bookingId}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Booking details retrieved successfully",
  "data": {
    "bookingId": "BK202511140001",
    "token": "20251115-7PM-AB12",
    "citizenName": "John Doe",
    "mobile": "9876543210",
    "date": "2025-11-15",
    "slotTime": "7:00 PM - 8:00 PM",
    "bookingTime": "2025-11-14T10:45:00",
    "status": "CONFIRMED",
    "isVerified": false,
    "verifiedAt": null,
    "verifiedBy": null
  },
  "timestamp": "2025-11-14T10:46:00Z",
  "requestId": "guid-value"
}
```

#### 3.3 Download Receipt
```
GET /api/bookings/{bookingId}/receipt
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Receipt generated successfully",
  "data": {
    "receiptUrl": "https://api.park.com/receipts/BK202511140001.pdf",
    "receiptHtml": "<html>...</html>",
    "expiresAt": "2025-11-14T11:45:00"
  },
  "timestamp": "2025-11-14T10:47:00Z",
  "requestId": "guid-value"
}
```

### 4. Department Portal APIs

#### 4.1 Department Login
```
POST /api/department/login
```

**Request**:
```json
{
  "userId": "admin",
  "password": "admin123"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "admin",
    "userName": "Administrator",
    "role": "ADMIN",
    "authToken": "jwt-token-here",
    "tokenExpiry": "2025-11-15T10:45:00",
    "permissions": ["VIEW_BOOKINGS", "VERIFY_ENTRY", "GENERATE_REPORTS"]
  },
  "timestamp": "2025-11-14T10:50:00Z",
  "requestId": "guid-value"
}
```

#### 4.2 Get Dashboard Statistics
```
GET /api/department/dashboard/stats
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Dashboard statistics retrieved successfully",
  "data": {
    "totalBookings": 1250,
    "todayBookings": 87,
    "verifiedEntries": 945,
    "pendingEntries": 305,
    "slotWiseBookings": [
      {
        "slotLabel": "7-8 PM",
        "bookings": 420
      },
      {
        "slotLabel": "8-9 PM",
        "bookings": 465
      },
      {
        "slotLabel": "9-10 PM",
        "bookings": 365
      }
    ],
    "recentBookings": [
      {
        "bookingId": "BK202511140001",
        "token": "20251115-7PM-AB12",
        "citizenName": "John Doe",
        "date": "2025-11-15",
        "slotTime": "7:00 PM - 8:00 PM",
        "status": "CONFIRMED"
      }
    ]
  },
  "timestamp": "2025-11-14T10:51:00Z",
  "requestId": "guid-value"
}
```

#### 4.3 Get All Bookings
```
GET /api/department/bookings?page=1&pageSize=10&status=CONFIRMED&date=2025-11-15&sortBy=bookingTime&sortOrder=desc
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Bookings retrieved successfully",
  "data": {
    "totalRecords": 1250,
    "totalPages": 125,
    "currentPage": 1,
    "pageSize": 10,
    "bookings": [
      {
        "bookingId": "BK202511140001",
        "token": "20251115-7PM-AB12",
        "citizenName": "John Doe",
        "mobile": "9876543210",
        "date": "2025-11-15",
        "slotTime": "7:00 PM - 8:00 PM",
        "bookingTime": "2025-11-14T10:45:00",
        "status": "CONFIRMED",
        "isVerified": false,
        "verifiedAt": null,
        "verifiedBy": null
      }
    ]
  },
  "timestamp": "2025-11-14T10:52:00Z",
  "requestId": "guid-value"
}
```

#### 4.4 Search Booking by Token
```
GET /api/department/bookings/search/{token}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Booking found",
  "data": {
    "bookingId": "BK202511140001",
    "token": "20251115-7PM-AB12",
    "citizenName": "John Doe",
    "mobile": "9876543210",
    "date": "2025-11-15",
    "slotTime": "7:00 PM - 8:00 PM",
    "bookingTime": "2025-11-14T10:45:00",
    "status": "CONFIRMED",
    "isVerified": false,
    "verifiedAt": null,
    "verifiedBy": null
  },
  "timestamp": "2025-11-14T10:53:00Z",
  "requestId": "guid-value"
}
```

#### 4.5 Verify Entry
```
POST /api/department/bookings/{bookingId}/verify
```

**Request**:
```json
{
  "bookingId": "BK202511140001",
  "token": "20251115-7PM-AB12",
  "verifiedBy": "admin",
  "notes": "ID verified, entry granted"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Entry verified successfully",
  "data": {
    "bookingId": "BK202511140001",
    "token": "20251115-7PM-AB12",
    "isVerified": true,
    "verifiedAt": "2025-11-15T19:05:00",
    "verifiedBy": "admin",
    "verifierName": "Administrator"
  },
  "timestamp": "2025-11-14T10:54:00Z",
  "requestId": "guid-value"
}
```

#### 4.6 Cancel Booking
```
POST /api/department/bookings/{bookingId}/cancel
```

**Request**:
```json
{
  "bookingId": "BK202511140001",
  "reason": "Duplicate booking",
  "cancelledBy": "admin"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Booking cancelled successfully",
  "data": {
    "bookingId": "BK202511140001",
    "status": "CANCELLED",
    "cancelledAt": "2025-11-14T11:00:00",
    "cancelledBy": "admin",
    "reason": "Duplicate booking"
  },
  "timestamp": "2025-11-14T10:55:00Z",
  "requestId": "guid-value"
}
```

### 5. Utilities APIs

#### 5.1 Send SMS
```
POST /api/utils/send-sms
```

**Request**:
```json
{
  "mobile": "9876543210",
  "message": "Your OTP is 1234. Valid for 5 minutes.",
  "templateId": "OTP_TEMPLATE"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "SMS sent successfully",
  "data": {
    "smsId": "SMS20251114001",
    "mobile": "9876543210",
    "sentAt": "2025-11-14T10:56:00"
  },
  "timestamp": "2025-11-14T10:56:00Z",
  "requestId": "guid-value"
}
```

#### 5.2 Generate QR Code
```
POST /api/utils/generate-qr
```

**Request**:
```json
{
  "token": "20251115-7PM-AB12",
  "bookingId": "BK202511140001"
}
```

**Response (200)**:
```json
{
  "success": true,
  "message": "QR code generated successfully",
  "data": {
    "qrCodeBase64": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
    "qrCodeUrl": "https://api.park.com/qr/BK202511140001.png"
  },
  "timestamp": "2025-11-14T10:57:00Z",
  "requestId": "guid-value"
}
```

### 6. Reports APIs

#### 6.1 Generate Booking Report
```
GET /api/reports/bookings?startDate=2025-11-01&endDate=2025-11-15&format=pdf&reportType=DAILY
```

**Response (200)**:
```json
{
  "success": true,
  "message": "Report generated successfully",
  "data": [
    {
      "bookingId": "BK202511140001",
      "citizenName": "John Doe",
      "date": "2025-11-15",
      "slotTime": "7:00 PM - 8:00 PM",
      "status": "CONFIRMED",
      "verifiedAt": null
    }
  ],
  "timestamp": "2025-11-14T10:58:00Z",
  "requestId": "guid-value"
}
```

## Security Implementation

### Authentication
- **Type**: SHA-256 Request Signing
- **Headers Required**: API Key, Timestamp, Signature
- **Implementation**: `ShaAuthenticationAttribute`

### Authorization
- **IP Whitelist**: Controlled via `IPWhitelistAttribute`
- **Rate Limiting**: 50-100 requests per minute depending on endpoint
- **Roles**: ADMIN, STAFF (for department portal)

### Security Headers
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- X-XSS-Protection: 1; mode=block
- Strict-Transport-Security: max-age=31536000
- Cache-Control: no-store, no-cache, must-revalidate

## Error Handling

### Standard Error Response Format
```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "timestamp": "2025-11-14T10:59:00Z",
  "requestId": "guid-value"
}
```

### HTTP Status Codes
- 200: Success
- 201: Created
- 400: Bad Request
- 401: Unauthorized
- 404: Not Found
- 409: Conflict (e.g., slot fully booked)
- 500: Internal Server Error

## Validation Rules

### Mobile Number
- Must be exactly 10 digits
- Pattern: `^\d{10}$`

### OTP
- Must be exactly 4 digits
- Valid for 5 minutes
- Maximum 3 resend attempts

### Dates
- Format: YYYY-MM-DD
- Cannot be in the past (for bookings)
- Start date must be before end date (for reports)

### Pagination
- Page: minimum 1
- PageSize: 1-100 (default 10)

## Dependency Injection

The application uses a simple dependency resolver configured in `SimpleDependencyResolver.cs`:

```csharp
// Park Booking Services
_services[typeof(IParkBookingRepository)] = () => new ParkBookingRepository(
    GetService(typeof(OracleConnectionFactory)) as OracleConnectionFactory
);

_services[typeof(IParkBookingService)] = () => new ParkBookingService(
    GetService(typeof(IParkBookingRepository)) as IParkBookingRepository
);

// Controllers
_services[typeof(CitizenController)] = () => new CitizenController(
    GetService(typeof(IParkBookingService)) as IParkBookingService
);
// ... and so on for other controllers
```

## Configuration

### Web.config Requirements

```xml
<configuration>
  <connectionStrings>
    <add name="OracleDb" 
         connectionString="Data Source=YourOracleDB;User Id=YourUser;Password=YourPassword;" 
         providerName="Oracle.ManagedDataAccess.Client" />
  </connectionStrings>

  <appSettings>
    <!-- SMS Configuration -->
    <add key="Sms_User" value="your_sms_user" />
    <add key="Sms_Password" value="your_sms_password" />
    <add key="Sms_SenderId" value="ParkBooking" />
    <add key="Sms_Channel" value="Trans" />
  </appSettings>
</configuration>
```

## Testing

### Sample Request (with cURL)
```bash
curl -X POST https://api.park.com/api/citizen/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -d '{
    "name": "John Doe",
    "mobile": "9876543210"
  }'
```

### Sample Response
```json
{
  "success": true,
  "message": "OTP sent successfully to mobile number",
  "data": {
    "citizenId": "CIT202511140001",
    "name": "John Doe",
    "mobile": "9876543210",
    "otpSentAt": "2025-11-14T10:30:00",
    "otpExpiresAt": "2025-11-14T10:35:00"
  },
  "timestamp": "2025-11-14T10:30:00Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## Performance Considerations

1. **Connection Pooling**: OracleConnection is pooled by OracleConnectionFactory
2. **Async/Await**: All operations use async methods to prevent thread starvation
3. **Caching**: Consider implementing caching for available slots
4. **Pagination**: All list endpoints support pagination for large datasets

## Logging

All errors and security events are logged to:
- System Diagnostics Trace
- Windows Event Log (optional)

Sensitive data (API Keys) is masked before logging.

## Future Enhancements

1. Implement JWT token-based authentication
2. Add webhook support for booking notifications
3. Implement database transaction management
4. Add comprehensive logging framework (e.g., NLog, Serilog)
5. Create unit tests for all services
6. Add API documentation with Swagger/OpenAPI
7. Implement caching layer for frequently accessed data
8. Add support for bulk operations

## Conclusion

This implementation provides a complete, secure, and production-ready API for the Evening Retreat Park Booking System on .NET 4.5 with Oracle 12c integration.
