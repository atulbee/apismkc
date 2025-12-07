# Duplicate Voter API - Postman Testing Guide

## ?? Files Included

1. **DuplicateVoterAPI.postman_collection.json** - Complete API collection with 9 endpoints
2. **DuplicateVoterAPI.postman_environment.json** - Environment variables configuration
3. **POSTMAN_TESTING_GUIDE.md** - This guide

---

## ?? Quick Start

### Step 1: Import Collection
1. Open Postman
2. Click **Import** button (top left)
3. Drag and drop `DuplicateVoterAPI.postman_collection.json`
4. Collection will appear in left sidebar

### Step 2: Import Environment
1. Click **Environments** icon (left sidebar)
2. Click **Import** button
3. Select `DuplicateVoterAPI.postman_environment.json`
4. Select the environment from dropdown (top right)

### Step 3: Configure Variables
1. Click on **Duplicate Voter API - Development** environment
2. Update these values:
   - `baseUrl`: Your API URL (e.g., `https://localhost:44300`)
   - `apiKey`: Your API key for authentication
   - `secretKey`: Your secret key for signature generation

---

## ?? API Endpoints Overview

### 1. **Find Duplicate Voters** ??
- **Method**: POST
- **URL**: `/api/voters/find-duplicates`
- **Purpose**: Search for potential duplicate voter records
- **Body**:
  ```json
  {
    "firstName": "Abhishek",
    "middleName": "Ajitkumar",
    "lastName": "Bedkihale"
  }
  ```
- **Response**: List of unverified voters matching search criteria

### 2. **Mark as Duplicates (TRUE)** ?
- **Method**: POST
- **URL**: `/api/voters/mark-duplicates`
- **Purpose**: Mark voters as duplicates of each other
- **Body**:
  ```json
  {
    "srNoArray": [201, 202, 203],
    "isDuplicate": true,
    "remarks": "Same person - Abhishek Ajitkumar Bedkihale"
  }
  ```
- **Response**: Duplication ID assigned to the group

### 3. **Mark as NOT Duplicates (FALSE)** ?
- **Method**: POST
- **URL**: `/api/voters/mark-duplicates`
- **Purpose**: Mark voters as different people (not duplicates)
- **Body**:
  ```json
  {
    "srNoArray": [204, 205],
    "isDuplicate": false,
    "remarks": "Different persons with similar names"
  }
  ```
- **Response**: Status confirmation with null duplication ID

### 4. **Get Verification Status** ??
- **Method**: GET
- **URL**: `/api/voters/verification-status`
- **Purpose**: Get overall statistics and progress
- **Response**:
  ```json
  {
    "totalRecords": 10000,
    "verifiedRecords": 7500,
    "unverifiedRecords": 2500,
    "duplicateRecords": 1200,
    "notDuplicateRecords": 6300,
    "verificationPercentage": 75.0
  }
  ```

### 5. **Get All Duplicate Groups** ??
- **Method**: GET
- **URL**: `/api/voters/duplicate-groups`
- **Purpose**: List all duplicate groups with their records
- **Response**: Array of duplicate groups with associated voters

### 6. **Get Duplicate Group by ID** ??
- **Method**: GET
- **URL**: `/api/voters/duplicate-groups?duplicationId=1001`
- **Purpose**: Get specific duplicate group details
- **Query Params**: `duplicationId` (optional)

### 7. **Get Voter by SR_NO** ??
- **Method**: GET
- **URL**: `/api/voters/{srNo}`
- **Purpose**: Get single voter record details
- **Example**: `/api/voters/201`
- **Response**: Complete voter record with duplicate flag and verification status

### 8. **Get Unverified Count** ??
- **Method**: GET
- **URL**: `/api/voters/unverified-count`
- **Purpose**: Get count of records pending review
- **Response**: `{ "unverifiedCount": 2500 }`

### 9. **Reset Verification (Admin)** ??
- **Method**: POST
- **URL**: `/api/voters/reset-verification`
- **Purpose**: **RESET ALL VERIFICATION DATA** (testing/admin only)
- **Response**: Count of records reset and groups deleted

---

## ?? Authentication

The API uses **SHA-256 based authentication**. Each request requires:

### Required Headers
```
Content-Type: application/json
X-API-Key: YOUR_API_KEY
X-Timestamp: 2025-01-14T10:30:00Z
X-Signature: COMPUTED_SHA256_SIGNATURE
```

### How Signature Works
1. Concatenate: `apiKey + timestamp + requestBody`
2. Compute SHA-256 hash
3. Add to `X-Signature` header

**Note**: The collection has pre-request scripts that auto-generate timestamps.

---

## ?? Testing Workflow

### Typical Testing Sequence

#### 1. Check Initial Status
```
GET /api/voters/verification-status
GET /api/voters/unverified-count
```

#### 2. Search for Duplicates
```
POST /api/voters/find-duplicates
Body: { "firstName": "Abhishek", "lastName": "Bedkihale" }
```

#### 3. Review Results
- Note the SR_NO values returned
- Decide which records are duplicates

#### 4. Mark as Duplicates
```
POST /api/voters/mark-duplicates
Body: { 
  "srNoArray": [201, 202], 
  "isDuplicate": true,
  "remarks": "Same person"
}
```

#### 5. Verify Group Created
```
GET /api/voters/duplicate-groups
```

#### 6. Get Individual Voter Details
```
GET /api/voters/201
```

#### 7. Check Progress
```
GET /api/voters/verification-status
GET /api/voters/unverified-count
```

---

## ?? Sample Test Cases

### Test Case 1: Mark 3 Voters as Duplicates
```json
POST /api/voters/mark-duplicates
{
  "srNoArray": [201, 202, 203],
  "isDuplicate": true,
  "remarks": "Same person - Abhishek Bedkihale"
}

Expected: 
- Status: 200
- Response.data.duplicationId: Valid number (e.g., 1001)
- Response.data.status: "SUCCESS"
```

### Test Case 2: Mark 2 Voters as NOT Duplicates
```json
POST /api/voters/mark-duplicates
{
  "srNoArray": [204, 205],
  "isDuplicate": false,
  "remarks": "Different persons"
}

Expected:
- Status: 200
- Response.data.duplicationId: null
- Response.data.status: "SUCCESS"
```

### Test Case 3: Search with Partial Name
```json
POST /api/voters/find-duplicates
{
  "firstName": "Abhi"
}

Expected:
- Status: 200
- Response.data.duplicateCount: >= 0
- Response.data.records: Array of unverified voters
```

### Test Case 4: Get Non-Existent Voter
```
GET /api/voters/99999

Expected:
- Status: 404
- Response.success: false
- Response.errorCode: "VOTER_NOT_FOUND"
```

---

## ? Automated Tests

The collection includes **pre-configured test scripts** that run automatically:

### Common Tests (All Requests)
```javascript
? Status code is 200
? Response has success field
? Response has timestamp
? Response has requestId
```

### To Run All Tests
1. Click on collection name
2. Click **Run** button
3. Select all requests
4. Click **Run Duplicate Voter API**
5. View results in test runner

---

## ?? Response Format

All endpoints return a standard response format:

### Success Response
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    // Endpoint-specific data
  },
  "timestamp": "2025-01-14T10:30:00Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errorCode": "ERROR_CODE",
  "timestamp": "2025-01-14T10:30:00Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## ?? Common Error Codes

| Code | Meaning | Resolution |
|------|---------|------------|
| `MISSING_REQUEST` | Request body is null | Provide request body |
| `MISSING_NAME_PARAMETER` | No name fields provided | Add firstName, middleName, or lastName |
| `MISSING_SR_NO_ARRAY` | SR_NO array is empty | Provide at least one SR_NO |
| `INSUFFICIENT_SR_NO_COUNT` | Less than 2 SR_NO for duplicates | Add more SR_NO values |
| `INVALID_SR_NO` | SR_NO is not positive | Use positive integers |
| `VOTER_NOT_FOUND` | Voter doesn't exist | Check SR_NO value |
| `INVALID_DUPLICATION_ID` | Invalid group ID | Use positive integer |

---

## ?? Expected Response Samples

### Find Duplicates Response
```json
{
  "success": true,
  "message": "Found 3 potential duplicate records",
  "data": {
    "duplicateCount": 3,
    "records": [
      {
        "srNo": 201,
        "wardDivNo": "001/002",
        "firstName": "Abhishek",
        "lastName": "Bedkihale",
        "relationFirstname": "Ajitkumar",
        "relationLastname": "Bedkihale",
        "relationType": "Father",
        "houseNo": "123",
        "age": 35,
        "sex": "M",
        "epicNumber": "ABC1234567",
        "voterSerialNo": "001",
        "duplicateFlag": "UNKNOWN",
        "verified": "FALSE",
        "duplicationId": null
      }
    ]
  }
}
```

### Verification Status Response
```json
{
  "success": true,
  "message": "Verification status retrieved successfully",
  "data": {
    "totalRecords": 10000,
    "verifiedRecords": 7500,
    "unverifiedRecords": 2500,
    "duplicateRecords": 1200,
    "notDuplicateRecords": 6300,
    "verificationPercentage": 75.0
  }
}
```

---

## ??? Troubleshooting

### Problem: 401 Unauthorized
**Solution**: 
- Check `X-API-Key` header is set
- Verify API key is correct
- Ensure signature is properly generated

### Problem: 400 Bad Request
**Solution**:
- Validate request body JSON format
- Check required fields are provided
- Review validation error message

### Problem: 404 Not Found
**Solution**:
- Verify SR_NO exists in database
- Check URL path is correct
- Ensure endpoint is deployed

### Problem: 500 Internal Server Error
**Solution**:
- Check Oracle database connection
- Verify stored procedures exist
- Review server logs for details

---

## ?? Additional Resources

### Oracle Procedures Required
```sql
- PROC_FIND_DUPLICATE_VOTERS
- PROC_MARK_DUPLICATES
- PROC_GET_VERIFICATION_STATUS
```

### Database Tables Required
```sql
- DUPLICATE_VOTERS
- DUPLICATION_RECORDS
```

### Configuration Required
```xml
<!-- Web.config -->
<connectionStrings>
  <add name="OracleDb" 
       connectionString="Data Source=...;User Id=ws;Password=..." />
</connectionStrings>
```

---

## ?? Best Practices

### 1. Test in Sequence
Start with verification status, then search, then mark duplicates

### 2. Use Descriptive Remarks
Always add meaningful remarks when marking duplicates/non-duplicates

### 3. Verify Before Marking
Review search results carefully before marking as duplicates

### 4. Check Progress Regularly
Use verification status endpoint to track progress

### 5. Use Reset Carefully
Only use reset endpoint in development/testing environments

### 6. Save Duplication IDs
Note the duplication IDs for future reference and reporting

---

## ?? Support

For issues or questions:
1. Review this guide thoroughly
2. Check server logs for errors
3. Verify Oracle database connection
4. Review API implementation documentation

---

## ?? Quick Test Script

Run this in Postman Console to test all endpoints:

```javascript
// 1. Get Status
pm.sendRequest({
    url: pm.environment.get('baseUrl') + '/api/voters/verification-status',
    method: 'GET'
}, (err, res) => {
    console.log('Verification Status:', res.json());
});

// 2. Find Duplicates
pm.sendRequest({
    url: pm.environment.get('baseUrl') + '/api/voters/find-duplicates',
    method: 'POST',
    header: { 'Content-Type': 'application/json' },
    body: { mode: 'raw', raw: JSON.stringify({
        firstName: 'Abhishek'
    })}
}, (err, res) => {
    console.log('Find Results:', res.json());
});

// 3. Get Unverified Count
pm.sendRequest({
    url: pm.environment.get('baseUrl') + '/api/voters/unverified-count',
    method: 'GET'
}, (err, res) => {
    console.log('Unverified Count:', res.json());
});
```

---

**Collection Version**: 1.0.0  
**Last Updated**: January 2025  
**API Framework**: .NET Framework 4.5  
**Database**: Oracle 12c  
**Status**: ? Ready for Testing
