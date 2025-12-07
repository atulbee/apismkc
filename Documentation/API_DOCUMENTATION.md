# The Evening Retreat Park - API Endpoints Documentation
## For Oracle 12c Procedures & .NET 4.5 Web API Implementation

---

## TABLE OF CONTENTS
1. [Citizen Portal APIs](#citizen-portal-apis)
2. [Department Portal APIs](#department-portal-apis)
3. [Common/Utility APIs](#commonutility-apis)
4. [Database Schema](#database-schema)
5. [Oracle Procedures](#oracle-procedures)
6. [.NET API Controllers](#net-api-controllers)

---

## CITIZEN PORTAL APIs

### 1. CITIZEN REGISTRATION
**Endpoint:** `POST /api/citizen/register`

**Description:** Registers a new citizen and sends OTP to mobile number.

**Request Body:**
```json
{
  "name": "John Doe",
  "mobile": "9876543210"
}
```

**Response (Success - 200):**
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
  }
}
```

**Response (Error - 400):**
```json
{
  "success": false,
  "message": "Invalid mobile number format",
  "errors": ["Mobile number must be 10 digits"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_CITIZEN_REGISTER (
  P_NAME IN VARCHAR2,
  P_MOBILE IN VARCHAR2,
  P_CITIZEN_ID OUT VARCHAR2,
  P_OTP OUT VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpPost]
[Route("api/citizen/register")]
public IHttpActionResult Register(CitizenRegistrationRequest request)
```

---

### 2. OTP VERIFICATION
**Endpoint:** `POST /api/citizen/verify-otp`

**Description:** Verifies the OTP sent to citizen's mobile number.

**Request Body:**
```json
{
  "citizenId": "CIT202511140001",
  "mobile": "9876543210",
  "otp": "1234"
}
```

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "OTP verified successfully",
  "data": {
    "citizenId": "CIT202511140001",
    "isVerified": true,
    "authToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenExpiry": "2025-11-14T22:30:00"
  }
}
```

**Response (Error - 401):**
```json
{
  "success": false,
  "message": "Invalid or expired OTP",
  "errors": ["OTP verification failed"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_VERIFY_OTP (
  P_CITIZEN_ID IN VARCHAR2,
  P_MOBILE IN VARCHAR2,
  P_OTP IN VARCHAR2,
  P_IS_VALID OUT NUMBER,
  P_AUTH_TOKEN OUT VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpPost]
[Route("api/citizen/verify-otp")]
public IHttpActionResult VerifyOTP(OTPVerificationRequest request)
```

---

### 3. RESEND OTP
**Endpoint:** `POST /api/citizen/resend-otp`

**Description:** Resends OTP to citizen's mobile number.

**Request Body:**
```json
{
  "citizenId": "CIT202511140001",
  "mobile": "9876543210"
}
```

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "OTP resent successfully",
  "data": {
    "otpSentAt": "2025-11-14T10:35:00",
    "otpExpiresAt": "2025-11-14T10:40:00",
    "attemptsRemaining": 2
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_RESEND_OTP (
  P_CITIZEN_ID IN VARCHAR2,
  P_MOBILE IN VARCHAR2,
  P_NEW_OTP OUT VARCHAR2,
  P_ATTEMPTS_LEFT OUT NUMBER,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpPost]
[Route("api/citizen/resend-otp")]
public IHttpActionResult ResendOTP(ResendOTPRequest request)
```

---

### 4. GET AVAILABLE SLOTS
**Endpoint:** `GET /api/slots/available?date=2025-11-15`

**Description:** Retrieves available time slots for a specific date.

**Query Parameters:**
- `date` (required): Date in YYYY-MM-DD format

**Response (Success - 200):**
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
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_GET_AVAILABLE_SLOTS (
  P_DATE IN DATE,
  P_SLOTS OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpGet]
[Route("api/slots/available")]
public IHttpActionResult GetAvailableSlots(string date)
```

---

### 5. CREATE BOOKING
**Endpoint:** `POST /api/bookings/create`

**Description:** Creates a new park slot booking.

**Request Headers:**
```
Authorization: Bearer {authToken}
```

**Request Body:**
```json
{
  "citizenId": "CIT202511140001",
  "date": "2025-11-15",
  "slotId": "SLOT001",
  "slotCode": "7PM"
}
```

**Response (Success - 201):**
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
  }
}
```

**Response (Error - 409):**
```json
{
  "success": false,
  "message": "Slot is fully booked",
  "errors": ["No seats available for selected slot"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_CREATE_BOOKING (
  P_CITIZEN_ID IN VARCHAR2,
  P_DATE IN DATE,
  P_SLOT_ID IN VARCHAR2,
  P_SLOT_CODE IN VARCHAR2,
  P_BOOKING_ID OUT VARCHAR2,
  P_TOKEN OUT VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize]
[HttpPost]
[Route("api/bookings/create")]
public IHttpActionResult CreateBooking(BookingRequest request)
```

---

### 6. GET BOOKING DETAILS
**Endpoint:** `GET /api/bookings/{bookingId}`

**Description:** Retrieves booking details by booking ID.

**Response (Success - 200):**
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
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_GET_BOOKING_DETAILS (
  P_BOOKING_ID IN VARCHAR2,
  P_BOOKING_DATA OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpGet]
[Route("api/bookings/{bookingId}")]
public IHttpActionResult GetBookingDetails(string bookingId)
```

---

### 7. DOWNLOAD RECEIPT
**Endpoint:** `GET /api/bookings/{bookingId}/receipt`

**Description:** Generates and downloads booking receipt.

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "Receipt generated successfully",
  "data": {
    "receiptUrl": "https://api.park.com/receipts/BK202511140001.pdf",
    "receiptHtml": "<html>...</html>",
    "expiresAt": "2025-11-14T11:45:00"
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_GENERATE_RECEIPT (
  P_BOOKING_ID IN VARCHAR2,
  P_RECEIPT_DATA OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpGet]
[Route("api/bookings/{bookingId}/receipt")]
public IHttpActionResult DownloadReceipt(string bookingId)
```

---

## DEPARTMENT PORTAL APIs

### 8. DEPARTMENT LOGIN
**Endpoint:** `POST /api/department/login`

**Description:** Authenticates department user and provides access token.

**Request Body:**
```json
{
  "userId": "admin",
  "password": "admin123"
}
```

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "userId": "admin",
    "userName": "Administrator",
    "role": "ADMIN",
    "authToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenExpiry": "2025-11-15T10:45:00",
    "permissions": ["VIEW_BOOKINGS", "VERIFY_ENTRY", "GENERATE_REPORTS"]
  }
}
```

**Response (Error - 401):**
```json
{
  "success": false,
  "message": "Invalid credentials",
  "errors": ["Username or password is incorrect"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_DEPARTMENT_LOGIN (
  P_USER_ID IN VARCHAR2,
  P_PASSWORD IN VARCHAR2,
  P_USER_DATA OUT SYS_REFCURSOR,
  P_AUTH_TOKEN OUT VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[HttpPost]
[Route("api/department/login")]
public IHttpActionResult Login(DepartmentLoginRequest request)
```

---

### 9. GET DASHBOARD STATISTICS
**Endpoint:** `GET /api/department/dashboard/stats`

**Description:** Retrieves dashboard statistics for department portal.

**Request Headers:**
```
Authorization: Bearer {authToken}
```

**Response (Success - 200):**
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
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_GET_DASHBOARD_STATS (
  P_USER_ID IN VARCHAR2,
  P_STATS_DATA OUT SYS_REFCURSOR,
  P_SLOT_DATA OUT SYS_REFCURSOR,
  P_RECENT_BOOKINGS OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN,STAFF")]
[HttpGet]
[Route("api/department/dashboard/stats")]
public IHttpActionResult GetDashboardStats()
```

---

### 10. GET ALL BOOKINGS
**Endpoint:** `GET /api/department/bookings`

**Description:** Retrieves paginated list of all bookings with filters.

**Query Parameters:**
- `page` (default: 1): Page number
- `pageSize` (default: 10): Items per page
- `status` (optional): CONFIRMED, VERIFIED, CANCELLED
- `date` (optional): Filter by date (YYYY-MM-DD)
- `searchToken` (optional): Search by token ID
- `sortBy` (default: bookingTime): Field to sort by
- `sortOrder` (default: desc): asc or desc

**Response (Success - 200):**
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
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_GET_ALL_BOOKINGS (
  P_PAGE IN NUMBER,
  P_PAGE_SIZE IN NUMBER,
  P_STATUS IN VARCHAR2,
  P_DATE IN DATE,
  P_SEARCH_TOKEN IN VARCHAR2,
  P_SORT_BY IN VARCHAR2,
  P_SORT_ORDER IN VARCHAR2,
  P_BOOKINGS OUT SYS_REFCURSOR,
  P_TOTAL_RECORDS OUT NUMBER,
  P_STATUS_CODE OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN,STAFF")]
[HttpGet]
[Route("api/department/bookings")]
public IHttpActionResult GetAllBookings([FromUri] BookingFilterRequest request)
```

---

### 11. SEARCH BOOKING BY TOKEN
**Endpoint:** `GET /api/department/bookings/search/{token}`

**Description:** Searches for a booking by token ID.

**Response (Success - 200):**
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
    "verifiedBy": null,
    "canVerify": true
  }
}
```

**Response (Error - 404):**
```json
{
  "success": false,
  "message": "Booking not found",
  "errors": ["No booking found with token: 20251115-7PM-AB12"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_SEARCH_BOOKING_BY_TOKEN (
  P_TOKEN IN VARCHAR2,
  P_BOOKING_DATA OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN,STAFF")]
[HttpGet]
[Route("api/department/bookings/search/{token}")]
public IHttpActionResult SearchBookingByToken(string token)
```

---

### 12. VERIFY ENTRY
**Endpoint:** `POST /api/department/bookings/{bookingId}/verify`

**Description:** Verifies citizen entry at park entrance.

**Request Headers:**
```
Authorization: Bearer {authToken}
```

**Request Body:**
```json
{
  "bookingId": "BK202511140001",
  "token": "20251115-7PM-AB12",
  "verifiedBy": "admin",
  "notes": "ID verified, entry granted"
}
```

**Response (Success - 200):**
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
  }
}
```

**Response (Error - 400):**
```json
{
  "success": false,
  "message": "Verification failed",
  "errors": ["Booking is already verified", "Entry time has expired"]
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_VERIFY_ENTRY (
  P_BOOKING_ID IN VARCHAR2,
  P_TOKEN IN VARCHAR2,
  P_VERIFIED_BY IN VARCHAR2,
  P_NOTES IN VARCHAR2,
  P_VERIFICATION_DATA OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN,STAFF")]
[HttpPost]
[Route("api/department/bookings/{bookingId}/verify")]
public IHttpActionResult VerifyEntry(string bookingId, VerifyEntryRequest request)
```

---

### 13. CANCEL BOOKING
**Endpoint:** `POST /api/department/bookings/{bookingId}/cancel`

**Description:** Cancels a booking (admin only).

**Request Body:**
```json
{
  "bookingId": "BK202511140001",
  "reason": "Duplicate booking",
  "cancelledBy": "admin"
}
```

**Response (Success - 200):**
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
  }
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_CANCEL_BOOKING (
  P_BOOKING_ID IN VARCHAR2,
  P_REASON IN VARCHAR2,
  P_CANCELLED_BY IN VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN")]
[HttpPost]
[Route("api/department/bookings/{bookingId}/cancel")]
public IHttpActionResult CancelBooking(string bookingId, CancelBookingRequest request)
```

---

## COMMON/UTILITY APIs

### 14. SEND SMS
**Endpoint:** `POST /api/utils/send-sms`

**Description:** Sends SMS notification (internal service).

**Request Body:**
```json
{
  "mobile": "9876543210",
  "message": "Your OTP is 1234. Valid for 5 minutes.",
  "templateId": "OTP_TEMPLATE"
}
```

**Oracle Procedure:**
```sql
PROCEDURE SP_LOG_SMS (
  P_MOBILE IN VARCHAR2,
  P_MESSAGE IN CLOB,
  P_TEMPLATE_ID IN VARCHAR2,
  P_SMS_ID OUT VARCHAR2,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

---

### 15. GENERATE QR CODE
**Endpoint:** `POST /api/utils/generate-qr`

**Description:** Generates QR code for booking token.

**Request Body:**
```json
{
  "token": "20251115-7PM-AB12",
  "bookingId": "BK202511140001"
}
```

**Response (Success - 200):**
```json
{
  "success": true,
  "message": "QR code generated successfully",
  "data": {
    "qrCodeBase64": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
    "qrCodeUrl": "https://api.park.com/qr/BK202511140001.png"
  }
}
```

---

### 16. GET REPORTS
**Endpoint:** `GET /api/reports/bookings`

**Description:** Generates booking reports with various filters.

**Query Parameters:**
- `startDate`: Start date (YYYY-MM-DD)
- `endDate`: End date (YYYY-MM-DD)
- `format`: pdf, excel, csv
- `reportType`: DAILY, WEEKLY, MONTHLY, CUSTOM

**Oracle Procedure:**
```sql
PROCEDURE SP_GENERATE_BOOKING_REPORT (
  P_START_DATE IN DATE,
  P_END_DATE IN DATE,
  P_REPORT_TYPE IN VARCHAR2,
  P_REPORT_DATA OUT SYS_REFCURSOR,
  P_STATUS OUT NUMBER,
  P_MESSAGE OUT VARCHAR2
)
```

**.NET Controller Method:**
```csharp
[Authorize(Roles = "ADMIN")]
[HttpGet]
[Route("api/reports/bookings")]
public IHttpActionResult GenerateBookingReport([FromUri] ReportRequest request)
```

---

## DATABASE SCHEMA

### Core Tables:

```sql
-- Citizens Table
CREATE TABLE TBL_CITIZENS (
  CITIZEN_ID VARCHAR2(50) PRIMARY KEY,
  NAME VARCHAR2(200) NOT NULL,
  MOBILE VARCHAR2(10) NOT NULL UNIQUE,
  EMAIL VARCHAR2(100),
  IS_VERIFIED NUMBER(1) DEFAULT 0,
  CREATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  UPDATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- OTP Table
CREATE TABLE TBL_OTP (
  OTP_ID VARCHAR2(50) PRIMARY KEY,
  CITIZEN_ID VARCHAR2(50) REFERENCES TBL_CITIZENS(CITIZEN_ID),
  MOBILE VARCHAR2(10) NOT NULL,
  OTP VARCHAR2(6) NOT NULL,
  CREATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  EXPIRES_AT TIMESTAMP NOT NULL,
  IS_USED NUMBER(1) DEFAULT 0,
  ATTEMPTS NUMBER DEFAULT 0
);

-- Time Slots Table
CREATE TABLE TBL_SLOTS (
  SLOT_ID VARCHAR2(50) PRIMARY KEY,
  SLOT_CODE VARCHAR2(10) NOT NULL UNIQUE,
  SLOT_TIME VARCHAR2(50) NOT NULL,
  SLOT_LABEL VARCHAR2(20) NOT NULL,
  START_TIME TIMESTAMP NOT NULL,
  END_TIME TIMESTAMP NOT NULL,
  CAPACITY NUMBER DEFAULT 100,
  IS_ACTIVE NUMBER(1) DEFAULT 1
);

-- Bookings Table
CREATE TABLE TBL_BOOKINGS (
  BOOKING_ID VARCHAR2(50) PRIMARY KEY,
  TOKEN VARCHAR2(50) NOT NULL UNIQUE,
  CITIZEN_ID VARCHAR2(50) REFERENCES TBL_CITIZENS(CITIZEN_ID),
  BOOKING_DATE DATE NOT NULL,
  SLOT_ID VARCHAR2(50) REFERENCES TBL_SLOTS(SLOT_ID),
  SLOT_CODE VARCHAR2(10) NOT NULL,
  BOOKING_TIME TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  STATUS VARCHAR2(20) DEFAULT 'CONFIRMED',
  IS_VERIFIED NUMBER(1) DEFAULT 0,
  VERIFIED_AT TIMESTAMP,
  VERIFIED_BY VARCHAR2(50),
  CANCELLED_AT TIMESTAMP,
  CANCELLED_BY VARCHAR2(50),
  CANCEL_REASON VARCHAR2(500)
);

-- Department Users Table
CREATE TABLE TBL_DEPT_USERS (
  USER_ID VARCHAR2(50) PRIMARY KEY,
  USER_NAME VARCHAR2(200) NOT NULL,
  PASSWORD_HASH VARCHAR2(500) NOT NULL,
  ROLE VARCHAR2(20) NOT NULL,
  IS_ACTIVE NUMBER(1) DEFAULT 1,
  CREATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  LAST_LOGIN TIMESTAMP
);

-- Audit Log Table
CREATE TABLE TBL_AUDIT_LOG (
  LOG_ID VARCHAR2(50) PRIMARY KEY,
  USER_ID VARCHAR2(50),
  ACTION VARCHAR2(100) NOT NULL,
  TABLE_NAME VARCHAR2(100),
  RECORD_ID VARCHAR2(50),
  OLD_VALUE CLOB,
  NEW_VALUE CLOB,
  IP_ADDRESS VARCHAR2(50),
  CREATED_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- SMS Log Table
CREATE TABLE TBL_SMS_LOG (
  SMS_ID VARCHAR2(50) PRIMARY KEY,
  MOBILE VARCHAR2(10) NOT NULL,
  MESSAGE CLOB NOT NULL,
  TEMPLATE_ID VARCHAR2(50),
  STATUS VARCHAR2(20),
  SENT_AT TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  RESPONSE CLOB
);
```

---

## ORACLE PROCEDURES (Detailed Implementation)

### Package Specification:
```sql
CREATE OR REPLACE PACKAGE PKG_PARK_BOOKING AS

  -- Citizen Registration
  PROCEDURE SP_CITIZEN_REGISTER (
    P_NAME IN VARCHAR2,
    P_MOBILE IN VARCHAR2,
    P_CITIZEN_ID OUT VARCHAR2,
    P_OTP OUT VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- OTP Verification
  PROCEDURE SP_VERIFY_OTP (
    P_CITIZEN_ID IN VARCHAR2,
    P_MOBILE IN VARCHAR2,
    P_OTP IN VARCHAR2,
    P_IS_VALID OUT NUMBER,
    P_AUTH_TOKEN OUT VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Resend OTP
  PROCEDURE SP_RESEND_OTP (
    P_CITIZEN_ID IN VARCHAR2,
    P_MOBILE IN VARCHAR2,
    P_NEW_OTP OUT VARCHAR2,
    P_ATTEMPTS_LEFT OUT NUMBER,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Get Available Slots
  PROCEDURE SP_GET_AVAILABLE_SLOTS (
    P_DATE IN DATE,
    P_SLOTS OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Create Booking
  PROCEDURE SP_CREATE_BOOKING (
    P_CITIZEN_ID IN VARCHAR2,
    P_DATE IN DATE,
    P_SLOT_ID IN VARCHAR2,
    P_SLOT_CODE IN VARCHAR2,
    P_BOOKING_ID OUT VARCHAR2,
    P_TOKEN OUT VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Get Booking Details
  PROCEDURE SP_GET_BOOKING_DETAILS (
    P_BOOKING_ID IN VARCHAR2,
    P_BOOKING_DATA OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Department Login
  PROCEDURE SP_DEPARTMENT_LOGIN (
    P_USER_ID IN VARCHAR2,
    P_PASSWORD IN VARCHAR2,
    P_USER_DATA OUT SYS_REFCURSOR,
    P_AUTH_TOKEN OUT VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Dashboard Statistics
  PROCEDURE SP_GET_DASHBOARD_STATS (
    P_USER_ID IN VARCHAR2,
    P_STATS_DATA OUT SYS_REFCURSOR,
    P_SLOT_DATA OUT SYS_REFCURSOR,
    P_RECENT_BOOKINGS OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Get All Bookings
  PROCEDURE SP_GET_ALL_BOOKINGS (
    P_PAGE IN NUMBER,
    P_PAGE_SIZE IN NUMBER,
    P_STATUS_FILTER IN VARCHAR2,
    P_DATE IN DATE,
    P_SEARCH_TOKEN IN VARCHAR2,
    P_SORT_BY IN VARCHAR2,
    P_SORT_ORDER IN VARCHAR2,
    P_BOOKINGS OUT SYS_REFCURSOR,
    P_TOTAL_RECORDS OUT NUMBER,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Search Booking by Token
  PROCEDURE SP_SEARCH_BOOKING_BY_TOKEN (
    P_TOKEN IN VARCHAR2,
    P_BOOKING_DATA OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Verify Entry
  PROCEDURE SP_VERIFY_ENTRY (
    P_BOOKING_ID IN VARCHAR2,
    P_TOKEN IN VARCHAR2,
    P_VERIFIED_BY IN VARCHAR2,
    P_NOTES IN VARCHAR2,
    P_VERIFICATION_DATA OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Cancel Booking
  PROCEDURE SP_CANCEL_BOOKING (
    P_BOOKING_ID IN VARCHAR2,
    P_REASON IN VARCHAR2,
    P_CANCELLED_BY IN VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Generate Receipt
  PROCEDURE SP_GENERATE_RECEIPT (
    P_BOOKING_ID IN VARCHAR2,
    P_RECEIPT_DATA OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Generate Report
  PROCEDURE SP_GENERATE_BOOKING_REPORT (
    P_START_DATE IN DATE,
    P_END_DATE IN DATE,
    P_REPORT_TYPE IN VARCHAR2,
    P_REPORT_DATA OUT SYS_REFCURSOR,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

  -- Log SMS
  PROCEDURE SP_LOG_SMS (
    P_MOBILE IN VARCHAR2,
    P_MESSAGE IN CLOB,
    P_TEMPLATE_ID IN VARCHAR2,
    P_SMS_ID OUT VARCHAR2,
    P_STATUS OUT NUMBER,
    P_MESSAGE OUT VARCHAR2
  );

END PKG_PARK_BOOKING;
/
```

---

## .NET API CONTROLLERS

### Base Controller Structure:
```csharp
using System;
using System.Net;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;

namespace ParkBookingAPI.Controllers
{
    [RoutePrefix("api")]
    public abstract class BaseController : ApiController
    {
        protected readonly string ConnectionString = 
            System.Configuration.ConfigurationManager
            .ConnectionStrings["OracleDB"].ConnectionString;

        protected ApiResponse<T> CreateResponse<T>(
            bool success, 
            string message, 
            T data = default(T), 
            string[] errors = null)
        {
            return new ApiResponse<T>
            {
                Success = success,
                Message = message,
                Data = data,
                Errors = errors
            };
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string[] Errors { get; set; }
    }
}
```

### Citizen Controller:
```csharp
namespace ParkBookingAPI.Controllers
{
    [RoutePrefix("api/citizen")]
    public class CitizenController : BaseController
    {
        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register(CitizenRegistrationRequest request)
        {
            try
            {
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand("PKG_PARK_BOOKING.SP_CITIZEN_REGISTER", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        
                        // Input parameters
                        cmd.Parameters.Add("P_NAME", OracleDbType.Varchar2).Value = request.Name;
                        cmd.Parameters.Add("P_MOBILE", OracleDbType.Varchar2).Value = request.Mobile;
                        
                        // Output parameters
                        cmd.Parameters.Add("P_CITIZEN_ID", OracleDbType.Varchar2, 50).Direction = 
                            System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add("P_OTP", OracleDbType.Varchar2, 6).Direction = 
                            System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add("P_STATUS", OracleDbType.Int32).Direction = 
                            System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add("P_MESSAGE", OracleDbType.Varchar2, 500).Direction = 
                            System.Data.ParameterDirection.Output;
                        
                        cmd.ExecuteNonQuery();
                        
                        int status = Convert.ToInt32(cmd.Parameters["P_STATUS"].Value.ToString());
                        string message = cmd.Parameters["P_MESSAGE"].Value.ToString();
                        
                        if (status == 1)
                        {
                            var response = new
                            {
                                CitizenId = cmd.Parameters["P_CITIZEN_ID"].Value.ToString(),
                                Name = request.Name,
                                Mobile = request.Mobile,
                                OtpSentAt = DateTime.Now,
                                OtpExpiresAt = DateTime.Now.AddMinutes(5)
                            };
                            
                            return Ok(CreateResponse(true, message, response));
                        }
                        else
                        {
                            return BadRequest(CreateResponse<object>(false, message, null, 
                                new[] { message }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("verify-otp")]
        public IHttpActionResult VerifyOTP(OTPVerificationRequest request)
        {
            // Similar implementation
        }
    }
}
```

---

## SECURITY CONSIDERATIONS

1. **Authentication**: Implement JWT tokens for API authentication
2. **Encryption**: Encrypt passwords using bcrypt or PBKDF2
3. **SQL Injection**: Use parameterized queries/stored procedures
4. **Rate Limiting**: Implement rate limiting for OTP endpoints
5. **HTTPS**: Use SSL/TLS for all API communications
6. **Input Validation**: Validate all inputs on server side
7. **Audit Logging**: Log all critical operations

---

## ERROR CODES

```
1000 - Success
2000 - Validation Error
2001 - Invalid Mobile Number
2002 - Invalid OTP
2003 - OTP Expired
3000 - Authentication Error
3001 - Unauthorized Access
4000 - Resource Not Found
4001 - Booking Not Found
4002 - Slot Not Found
5000 - Business Logic Error
5001 - Slot Fully Booked
5002 - Booking Already Verified
5003 - Duplicate Booking
6000 - Internal Server Error
7000 - Database Error
8000 - External Service Error
```

---

## DEPLOYMENT NOTES

1. Configure Oracle 12c connection string in web.config
2. Install Oracle.ManagedDataAccess.Client NuGet package
3. Set up SMS gateway integration for OTP
4. Configure CORS for frontend integration
5. Set up SSL certificate
6. Create database indexes for performance
7. Configure application pool for .NET 4.5
8. Set up backup and recovery procedures

---

This documentation provides a complete API specification for implementing the park booking system with Oracle 12c stored procedures and .NET 4.5 Web API.