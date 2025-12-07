# Test Voter API - PowerShell Script
# This script tests the Duplicate Voter API endpoints with proper SHA-256 authentication

param(
    [string]$BaseUrl = "http://localhost:57031",
    [string]$ApiKey = "TEST_API_KEY_12345678901234567890123456789012",
    [string]$SecretKey = "TEST_SECRET_KEY_67890ABCDEFGHIJ1234567890",
    [string]$Endpoint = "verification-status",
    [string]$Method = "GET",
    [string]$Body = ""
)

# Function to compute SHA-256 hash
function Get-Sha256Hash {
    param([string]$Text)
    
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $hash = $sha256.ComputeHash($bytes)
    $hashString = [System.BitConverter]::ToString($hash).Replace("-", "").ToLower()
    
    return $hashString
}

# Function to compute HMAC-SHA256 signature
function Get-HmacSha256Signature {
    param(
        [string]$Message,
        [string]$SecretKey
    )
    
    $hmacsha = New-Object System.Security.Cryptography.HMACSHA256
    $hmacsha.Key = [Text.Encoding]::UTF8.GetBytes($SecretKey)
    $hashBytes = $hmacsha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Message))
    $signature = [Convert]::ToBase64String($hashBytes)
    
    return $signature
}

# Function to make API request
function Invoke-VoterApi {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [string]$RequestBody = ""
    )
    
    # Generate Unix timestamp
    $unixTime = [Math]::Floor([decimal](Get-Date(Get-Date).ToUniversalTime()-uformat "%s"))
    $timestamp = $unixTime.ToString()
    
    # Get HTTP method and request URI
    $httpMethod = $Method.ToUpper()
    $requestUri = "/api/voters/$Endpoint"
    
    # Create signature string: HTTP_METHOD + REQUEST_URI + REQUEST_BODY + TIMESTAMP + API_KEY
    $stringToSign = $httpMethod + $requestUri + $RequestBody + $timestamp + $ApiKey
    
    # Compute HMAC-SHA256 signature
    $signature = Get-HmacSha256Signature -Message $stringToSign -SecretKey $SecretKey
    
    # Build URL
    $url = "$BaseUrl/api/voters/$Endpoint"
    
    # Create headers
    $headers = @{
        "Content-Type" = "application/json"
        "X-API-Key" = $ApiKey
        "X-Timestamp" = $timestamp
        "X-Signature" = $signature
    }
    
    # Display request info
    Write-Host "`n=== API Request ===" -ForegroundColor Cyan
    Write-Host "Method: $Method" -ForegroundColor Yellow
    Write-Host "URL: $url" -ForegroundColor Yellow
    Write-Host "Timestamp: $timestamp" -ForegroundColor Yellow
    Write-Host "Signature: $signature" -ForegroundColor Yellow
    
    if ($RequestBody) {
        Write-Host "Body: $RequestBody" -ForegroundColor Yellow
    }
    
    Write-Host "==================`n" -ForegroundColor Cyan
    
    try {
        # Make request
        if ($Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $url -Method $Method -Headers $headers -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $url -Method $Method -Headers $headers -Body $RequestBody -ErrorAction Stop
        }
        
        # Display response
        Write-Host "=== API Response ===" -ForegroundColor Green
        $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White
        Write-Host "===================`n" -ForegroundColor Green
        
        return $response
    }
    catch {
        Write-Host "=== API Error ===" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        
        if ($_.ErrorDetails.Message) {
            $_.ErrorDetails.Message | ConvertFrom-Json | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor Red
        }
        
        Write-Host "================`n" -ForegroundColor Red
        
        return $null
    }
}

# Main execution
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  Voter API Testing Script" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

# If specific endpoint and method provided, test that
if ($Endpoint -and $Method) {
    Invoke-VoterApi -Endpoint $Endpoint -Method $Method -RequestBody $Body
    exit
}

# Otherwise run all test scenarios
Write-Host "Running all test scenarios...`n" -ForegroundColor Cyan

# Test 1: Get Verification Status
Write-Host "`n[Test 1] Get Verification Status" -ForegroundColor Yellow
Invoke-VoterApi -Endpoint "verification-status" -Method "GET"
Start-Sleep -Seconds 1

# Test 2: Get Unverified Count
Write-Host "`n[Test 2] Get Unverified Count" -ForegroundColor Yellow
Invoke-VoterApi -Endpoint "unverified-count" -Method "GET"
Start-Sleep -Seconds 1

# Test 3: Find Duplicates
Write-Host "`n[Test 3] Find Duplicate Voters" -ForegroundColor Yellow
$findBody = @{
    firstName = "Abhishek"
    lastName = "Bedkihale"
} | ConvertTo-Json -Compress
Invoke-VoterApi -Endpoint "find-duplicates" -Method "POST" -RequestBody $findBody
Start-Sleep -Seconds 1

# Test 4: Get Voter by SR_NO (example)
Write-Host "`n[Test 4] Get Voter by SR_NO" -ForegroundColor Yellow
Invoke-VoterApi -Endpoint "201" -Method "GET"
Start-Sleep -Seconds 1

# Test 5: Get All Duplicate Groups
Write-Host "`n[Test 5] Get All Duplicate Groups" -ForegroundColor Yellow
Invoke-VoterApi -Endpoint "duplicate-groups" -Method "GET"

Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  All Tests Completed!" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Magenta

# Usage Examples
Write-Host "`n=== Usage Examples ===" -ForegroundColor Cyan
Write-Host "Get verification status:" -ForegroundColor White
Write-Host '  .\Test-VoterAPI.ps1 -Endpoint "verification-status" -Method "GET"' -ForegroundColor Gray
Write-Host "`nFind duplicates:" -ForegroundColor White
Write-Host '  .\Test-VoterAPI.ps1 -Endpoint "find-duplicates" -Method "POST" -Body ''{"firstName":"Abhishek"}''' -ForegroundColor Gray
Write-Host "`nGet voter by SR_NO:" -ForegroundColor White
Write-Host '  .\Test-VoterAPI.ps1 -Endpoint "201" -Method "GET"' -ForegroundColor Gray
Write-Host "`nRun all tests:" -ForegroundColor White
Write-Host '  .\Test-VoterAPI.ps1' -ForegroundColor Gray
Write-Host "======================`n" -ForegroundColor Cyan
