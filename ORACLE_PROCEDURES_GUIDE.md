# ??? Oracle Stored Procedure Requirements for Voter API

## ?? IMPORTANT: Column Mapping Issue Fixed

The error you encountered was due to **missing columns** in the stored procedure result set. The code now handles missing columns gracefully, but for best results, your Oracle procedures should return all expected columns.

---

## ?? Required Oracle Stored Procedures

### 1. PROC_FIND_DUPLICATE_VOTERS

**Purpose**: Search for potential duplicate voter records (returns unverified records only)

**Parameters**:
```sql
CREATE OR REPLACE PROCEDURE PROC_FIND_DUPLICATE_VOTERS (
    P_FIRST_NAME      IN VARCHAR2,
    P_MIDDLE_NAME     IN VARCHAR2,
    P_LAST_NAME       IN VARCHAR2,
    P_RESULT_CURSOR   OUT SYS_REFCURSOR,
    P_DUPLICATE_COUNT OUT NUMBER
)
```

**Required Columns in P_RESULT_CURSOR**:
```sql
SELECT 
    SR_NO,                  -- NUMBER (Required)
    WARD_DIV_NO,           -- VARCHAR2 (Optional)
    FIRST_NAME,            -- VARCHAR2 (Required)
    LAST_NAME,             -- VARCHAR2 (Optional)
    RELATION_FIRSTNAME,    -- VARCHAR2 (Optional)
    RELATION_LASTNAME,     -- VARCHAR2 (Optional)
    RELATION_TYPE,         -- VARCHAR2 (Optional)
    HOUSE_NO,              -- VARCHAR2 (Optional)
    AGE,                   -- NUMBER (Optional)
    SEX,                   -- VARCHAR2 (Optional)
    EPIC_NUMBER,           -- VARCHAR2 (Optional)
    VOTER_SERIAL_NO,       -- VARCHAR2 (Optional)
    DUPLICATE_FLAG,        -- VARCHAR2 (Optional)
    VERIFIED,              -- VARCHAR2 (Optional)
    DUPLICATION_ID         -- NUMBER (Optional)
FROM DUPLICATE_VOTERS
WHERE VERIFIED = 'FALSE' OR VERIFIED IS NULL
  AND (
      (P_FIRST_NAME IS NOT NULL AND UPPER(FIRST_NAME) LIKE '%' || UPPER(P_FIRST_NAME) || '%')
      OR (P_MIDDLE_NAME IS NOT NULL AND UPPER(MIDDLE_NAME) LIKE '%' || UPPER(P_MIDDLE_NAME) || '%')
      OR (P_LAST_NAME IS NOT NULL AND UPPER(LAST_NAME) LIKE '%' || UPPER(P_LAST_NAME) || '%')
  );
```

**Example Implementation**:
```sql
CREATE OR REPLACE PROCEDURE PROC_FIND_DUPLICATE_VOTERS (
    P_FIRST_NAME      IN VARCHAR2,
    P_MIDDLE_NAME     IN VARCHAR2,
    P_LAST_NAME       IN VARCHAR2,
    P_RESULT_CURSOR   OUT SYS_REFCURSOR,
    P_DUPLICATE_COUNT OUT NUMBER
)
AS
BEGIN
    -- Open cursor with results
    OPEN P_RESULT_CURSOR FOR
        SELECT 
            SR_NO,
            WARD_DIV_NO,
            FIRST_NAME,
            LAST_NAME,
            RELATION_FIRSTNAME,
            RELATION_LASTNAME,
            RELATION_TYPE,
            HOUSE_NO,
            AGE,
            SEX,
            EPIC_NUMBER,
            VOTER_SERIAL_NO,
            DUPLICATE_FLAG,
            VERIFIED,
            DUPLICATION_ID
        FROM DUPLICATE_VOTERS
        WHERE (VERIFIED = 'FALSE' OR VERIFIED IS NULL)
          AND (
              (P_FIRST_NAME IS NOT NULL AND UPPER(FIRST_NAME) LIKE '%' || UPPER(P_FIRST_NAME) || '%')
              OR (P_MIDDLE_NAME IS NOT NULL AND UPPER(MIDDLE_NAME) LIKE '%' || UPPER(P_MIDDLE_NAME) || '%')
              OR (P_LAST_NAME IS NOT NULL AND UPPER(LAST_NAME) LIKE '%' || UPPER(P_LAST_NAME) || '%')
          )
        ORDER BY SR_NO;
    
    -- Get count
    SELECT COUNT(*) INTO P_DUPLICATE_COUNT
    FROM DUPLICATE_VOTERS
    WHERE (VERIFIED = 'FALSE' OR VERIFIED IS NULL)
      AND (
          (P_FIRST_NAME IS NOT NULL AND UPPER(FIRST_NAME) LIKE '%' || UPPER(P_FIRST_NAME) || '%')
          OR (P_MIDDLE_NAME IS NOT NULL AND UPPER(MIDDLE_NAME) LIKE '%' || UPPER(P_MIDDLE_NAME) || '%')
          OR (P_LAST_NAME IS NOT NULL AND UPPER(LAST_NAME) LIKE '%' || UPPER(P_LAST_NAME) || '%')
      );
      
EXCEPTION
    WHEN OTHERS THEN
        P_DUPLICATE_COUNT := 0;
        RAISE;
END PROC_FIND_DUPLICATE_VOTERS;
/
```

---

### 2. PROC_MARK_DUPLICATES

**Purpose**: Mark voters as duplicates or not duplicates

**Parameters**:
```sql
CREATE OR REPLACE PROCEDURE PROC_MARK_DUPLICATES (
    P_SR_NO_ARRAY    IN SYS.ODCINUMBERLIST,  -- Array of SR_NO values
    P_IS_DUPLICATE   IN VARCHAR2,             -- 'TRUE' or 'FALSE'
    P_REMARKS        IN VARCHAR2,             -- Optional notes
    P_DUPLICATION_ID OUT NUMBER,              -- Group ID (null if not duplicate)
    P_STATUS         OUT VARCHAR2             -- SUCCESS or ERROR message
)
```

**Example Implementation**:
```sql
CREATE OR REPLACE PROCEDURE PROC_MARK_DUPLICATES (
    P_SR_NO_ARRAY    IN SYS.ODCINUMBERLIST,
    P_IS_DUPLICATE   IN VARCHAR2,
    P_REMARKS        IN VARCHAR2,
    P_DUPLICATION_ID OUT NUMBER,
    P_STATUS         OUT VARCHAR2
)
AS
    V_DUPLICATION_ID NUMBER;
    V_SR_NO_LIST VARCHAR2(4000);
BEGIN
    IF P_IS_DUPLICATE = 'TRUE' THEN
        -- Create new duplication group
        SELECT NVL(MAX(DUPLICATION_ID), 0) + 1 INTO V_DUPLICATION_ID FROM DUPLICATION_RECORDS;
        
        -- Build comma-separated list of SR_NO values
        FOR i IN 1..P_SR_NO_ARRAY.COUNT LOOP
            IF i = 1 THEN
                V_SR_NO_LIST := TO_CHAR(P_SR_NO_ARRAY(i));
            ELSE
                V_SR_NO_LIST := V_SR_NO_LIST || ',' || TO_CHAR(P_SR_NO_ARRAY(i));
            END IF;
        END LOOP;
        
        -- Insert into DUPLICATION_RECORDS
        INSERT INTO DUPLICATION_RECORDS (
            DUPLICATION_ID, SR_NO_LIST, MARKED_DATE, MARKED_BY, REMARKS
        ) VALUES (
            V_DUPLICATION_ID, V_SR_NO_LIST, SYSDATE, USER, P_REMARKS
        );
        
        -- Update voters
        FOR i IN 1..P_SR_NO_ARRAY.COUNT LOOP
            UPDATE DUPLICATE_VOTERS
            SET DUPLICATE_FLAG = 'DUPLICATE',
                VERIFIED = 'TRUE',
                DUPLICATION_ID = V_DUPLICATION_ID
            WHERE SR_NO = P_SR_NO_ARRAY(i);
        END LOOP;
        
        P_DUPLICATION_ID := V_DUPLICATION_ID;
        P_STATUS := 'SUCCESS: Marked ' || P_SR_NO_ARRAY.COUNT || ' voters as duplicates';
        
    ELSE
        -- Mark as NOT duplicates
        FOR i IN 1..P_SR_NO_ARRAY.COUNT LOOP
            UPDATE DUPLICATE_VOTERS
            SET DUPLICATE_FLAG = 'NOT_DUPLICATE',
                VERIFIED = 'TRUE',
                DUPLICATION_ID = NULL
            WHERE SR_NO = P_SR_NO_ARRAY(i);
        END LOOP;
        
        P_DUPLICATION_ID := NULL;
        P_STATUS := 'SUCCESS: Marked ' || P_SR_NO_ARRAY.COUNT || ' voters as not duplicates';
    END IF;
    
    COMMIT;
    
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        P_STATUS := 'ERROR: ' || SQLERRM;
        P_DUPLICATION_ID := NULL;
END PROC_MARK_DUPLICATES;
/
```

---

### 3. PROC_GET_VERIFICATION_STATUS

**Purpose**: Get overall verification statistics

**Parameters**:
```sql
CREATE OR REPLACE PROCEDURE PROC_GET_VERIFICATION_STATUS (
    P_TOTAL_RECORDS           OUT NUMBER,
    P_VERIFIED_RECORDS        OUT NUMBER,
    P_UNVERIFIED_RECORDS      OUT NUMBER,
    P_DUPLICATE_RECORDS       OUT NUMBER,
    P_NOT_DUPLICATE_RECORDS   OUT NUMBER,
    P_VERIFICATION_PERCENTAGE OUT NUMBER
)
```

**Example Implementation**:
```sql
CREATE OR REPLACE PROCEDURE PROC_GET_VERIFICATION_STATUS (
    P_TOTAL_RECORDS           OUT NUMBER,
    P_VERIFIED_RECORDS        OUT NUMBER,
    P_UNVERIFIED_RECORDS      OUT NUMBER,
    P_DUPLICATE_RECORDS       OUT NUMBER,
    P_NOT_DUPLICATE_RECORDS   OUT NUMBER,
    P_VERIFICATION_PERCENTAGE OUT NUMBER
)
AS
BEGIN
    -- Total records
    SELECT COUNT(*) INTO P_TOTAL_RECORDS FROM DUPLICATE_VOTERS;
    
    -- Verified records
    SELECT COUNT(*) INTO P_VERIFIED_RECORDS 
    FROM DUPLICATE_VOTERS 
    WHERE VERIFIED = 'TRUE';
    
    -- Unverified records
    SELECT COUNT(*) INTO P_UNVERIFIED_RECORDS 
    FROM DUPLICATE_VOTERS 
    WHERE VERIFIED = 'FALSE' OR VERIFIED IS NULL;
    
    -- Duplicate records
    SELECT COUNT(*) INTO P_DUPLICATE_RECORDS 
    FROM DUPLICATE_VOTERS 
    WHERE DUPLICATE_FLAG = 'DUPLICATE';
    
    -- Not duplicate records
    SELECT COUNT(*) INTO P_NOT_DUPLICATE_RECORDS 
    FROM DUPLICATE_VOTERS 
    WHERE DUPLICATE_FLAG = 'NOT_DUPLICATE';
    
    -- Calculate percentage
    IF P_TOTAL_RECORDS > 0 THEN
        P_VERIFICATION_PERCENTAGE := ROUND((P_VERIFIED_RECORDS / P_TOTAL_RECORDS) * 100, 2);
    ELSE
        P_VERIFICATION_PERCENTAGE := 0;
    END IF;
    
EXCEPTION
    WHEN OTHERS THEN
        P_TOTAL_RECORDS := 0;
        P_VERIFIED_RECORDS := 0;
        P_UNVERIFIED_RECORDS := 0;
        P_DUPLICATE_RECORDS := 0;
        P_NOT_DUPLICATE_RECORDS := 0;
        P_VERIFICATION_PERCENTAGE := 0;
END PROC_GET_VERIFICATION_STATUS;
/
```

---

## ?? Required Database Tables

### Table 1: DUPLICATE_VOTERS

```sql
CREATE TABLE DUPLICATE_VOTERS (
    SR_NO               NUMBER PRIMARY KEY,
    WARD_DIV_NO         VARCHAR2(50),
    FIRST_NAME          VARCHAR2(100),
    MIDDLE_NAME         VARCHAR2(100),
    LAST_NAME           VARCHAR2(100),
    RELATION_FIRSTNAME  VARCHAR2(100),
    RELATION_LASTNAME   VARCHAR2(100),
    RELATION_TYPE       VARCHAR2(50),
    HOUSE_NO            VARCHAR2(50),
    AGE                 NUMBER,
    SEX                 VARCHAR2(10),
    EPIC_NUMBER         VARCHAR2(50),
    VOTER_SERIAL_NO     VARCHAR2(50),
    DUPLICATE_FLAG      VARCHAR2(20) DEFAULT 'UNKNOWN',  -- 'UNKNOWN', 'DUPLICATE', 'NOT_DUPLICATE'
    VERIFIED            VARCHAR2(10) DEFAULT 'FALSE',    -- 'TRUE', 'FALSE'
    DUPLICATION_ID      NUMBER
);

CREATE INDEX IDX_DV_VERIFIED ON DUPLICATE_VOTERS(VERIFIED);
CREATE INDEX IDX_DV_DUPLICATE_FLAG ON DUPLICATE_VOTERS(DUPLICATE_FLAG);
CREATE INDEX IDX_DV_DUPLICATION_ID ON DUPLICATE_VOTERS(DUPLICATION_ID);
CREATE INDEX IDX_DV_NAME ON DUPLICATE_VOTERS(FIRST_NAME, LAST_NAME);
```

### Table 2: DUPLICATION_RECORDS

```sql
CREATE TABLE DUPLICATION_RECORDS (
    DUPLICATION_ID  NUMBER PRIMARY KEY,
    SR_NO_LIST      VARCHAR2(4000),
    MARKED_DATE     DATE DEFAULT SYSDATE,
    MARKED_BY       VARCHAR2(100),
    REMARKS         VARCHAR2(500)
);

CREATE INDEX IDX_DR_MARKED_DATE ON DUPLICATION_RECORDS(MARKED_DATE);
```

---

## ?? What Was Fixed in the Code

### Before (Would Crash):
```csharp
// Direct column access - crashes if column missing
SrNo = Convert.ToInt32(reader["SR_NO"])
```

### After (Safe):
```csharp
// Safe helper method - returns default if column missing
SrNo = GetInt32OrDefault(reader, "SR_NO")
```

### New Helper Methods Added:
1. `GetStringOrDefault()` - Returns `null` if column missing
2. `GetInt32OrDefault()` - Returns `0` if column missing
3. `GetNullableInt32()` - Returns `null` if column missing

---

## ? Testing the Procedures

### Test PROC_FIND_DUPLICATE_VOTERS:
```sql
DECLARE
    v_cursor SYS_REFCURSOR;
    v_count NUMBER;
BEGIN
    PROC_FIND_DUPLICATE_VOTERS('Abhishek', 'Ajitkumar', 'Bedkihale', v_cursor, v_count);
    DBMS_OUTPUT.PUT_LINE('Count: ' || v_count);
END;
/
```

### Test PROC_MARK_DUPLICATES:
```sql
DECLARE
    v_sr_array SYS.ODCINUMBERLIST := SYS.ODCINUMBERLIST(201, 202, 203);
    v_dup_id NUMBER;
    v_status VARCHAR2(500);
BEGIN
    PROC_MARK_DUPLICATES(v_sr_array, 'TRUE', 'Test remark', v_dup_id, v_status);
    DBMS_OUTPUT.PUT_LINE('Duplication ID: ' || v_dup_id);
    DBMS_OUTPUT.PUT_LINE('Status: ' || v_status);
END;
/
```

### Test PROC_GET_VERIFICATION_STATUS:
```sql
DECLARE
    v_total NUMBER;
    v_verified NUMBER;
    v_unverified NUMBER;
    v_duplicate NUMBER;
    v_not_dup NUMBER;
    v_percentage NUMBER;
BEGIN
    PROC_GET_VERIFICATION_STATUS(v_total, v_verified, v_unverified, v_duplicate, v_not_dup, v_percentage);
    DBMS_OUTPUT.PUT_LINE('Total: ' || v_total);
    DBMS_OUTPUT.PUT_LINE('Verified: ' || v_verified);
    DBMS_OUTPUT.PUT_LINE('Percentage: ' || v_percentage || '%');
END;
/
```

---

## ?? Summary

### What Caused the Error:
- ? The stored procedure didn't return all expected columns
- ? The code tried to access a missing column by name
- ? Oracle threw `GetColumnOrdinal` exception

### How It's Fixed:
- ? Code now safely checks if column exists before reading
- ? Returns default values for missing columns
- ? Won't crash even if procedure returns partial data

### Best Practice:
- ? Create procedures that return ALL columns listed above
- ? Use the example implementations provided
- ? Test procedures independently before API testing

---

## ?? Next Steps

1. **Create the tables** using the DDL above
2. **Deploy the stored procedures** using the examples
3. **Insert test data** into DUPLICATE_VOTERS
4. **Test the API** - it will now handle missing columns gracefully

---

**Status**: ? Code Fixed - Won't crash on missing columns  
**Recommendation**: Deploy complete stored procedures for full functionality
